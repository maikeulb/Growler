namespace Wall

module Domain = 
  open User
  open Tweet
  open Chessie.ErrorHandling
  open System
  open Chessie

  type NotifyTweet = Tweet -> AsyncResult<unit, Exception>

  type PublishTweetError =
  | CreateTweetError of Exception
  | NotifyTweetError of (TweetId * Exception)

  type PublishTweet =
    CreateTweet -> NotifyTweet -> 
      User -> Post -> AsyncResult<TweetId, PublishTweetError>

  let publishTweet createTweet notifyTweet (user : User) post = asyncTrial {
    let! tweetId = 
      createTweet user.UserId post
      |> AR.mapFailure CreateTweetError

    let tweet = {
      Id = tweetId
      UserId = user.UserId
      Username = user.Username
      Post = post
    }
    do! notifyTweet tweet 
        |> AR.mapFailure (fun ex -> NotifyTweetError(tweetId, ex))

    return tweetId
  }

module GetStream = 
  open Tweet
  open User
  open Stream
  open Chessie.ErrorHandling

  let mapStreamResponse response =
    match response with
    | Choice1Of2 _ -> ok ()
    | Choice2Of2 ex -> fail ex
  let notifyTweet (getStreamClient: GetStream.Client) (tweet : Tweet) = 
    
    let (UserId userId) = tweet.UserId
    let (TweetId tweetId) = tweet.Id
    let userFeed =
      GetStream.userFeed getStreamClient userId
    
    let activity = new Activity(userId.ToString(), "tweet", tweetId.ToString())
    activity.SetData("tweet", tweet.Post.Value)
    activity.SetData("username", tweet.Username.Value)
    
    userFeed.AddActivity(activity)
    |> Async.AwaitTask
    |> Async.Catch
    |> Async.map mapStreamResponse
    |> AR


module Suave =
  open Suave
  open Suave.Filters
  open Suave.Operators
  open User
  open Auth.Suave
  open Suave.DotLiquid
  open Tweet
  open Chiron
  open Chessie.ErrorHandling
  open Chessie
  open Domain

  type WallViewModel = {
    Username :  string
  }

  type PostRequest = PostRequest of string with
    static member FromJson (_ : PostRequest) = json {
      let! post = Json.read "post"
      return PostRequest post 
    }

  let renderWall (user : User) context = async {
    let vm = {Username = user.Username.Value }
    return! page "main/wall.liquid" vm context
  }

  let onPublishTweetSuccess (TweetId id) = 
    ["id", String (id.ToString())]
    |> Map.ofList
    |> Object
    |> JSON.ok

  let handleNewTweet publishTweet (user : User) context = async {
    match JSON.deserialize context.request  with
    | Success (PostRequest post) -> 
      match Post.TryCreate post with
      | Success post -> 
        let! webpart = 
          publishTweet user post
          |> AR.either onPublishTweetSuccess onPublishTweetFailure
        return! webpart context
      | Failure err -> 
        return! JSON.badRequest err context
    | Failure err -> 
      return! JSON.badRequest err context
  }
  
  
  let webpart getDataContext getStreamClient =
    let createTweet = Persistence.createTweet getDataContext 
    let notifyTweet = GetStream.notifyTweet getStreamClient
    let publishTweet = publishTweet createTweet notifyTweet
    choose [
      path "/wall" >=> requiresAuth (renderWall getStreamClient)
      POST >=> path "/tweets"  
        >=> requiresAuth2 (handleNewTweet publishTweet)  
    ]
