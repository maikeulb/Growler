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

  let onCreateTweetSuccess (TweetId id) = 
    ["id", String (id.ToString())]
    |> Map.ofList
    |> Object
    |> JSON.ok

  let onCreateTweetFailure (ex : System.Exception) =
    printfn "%A" ex
    JSON.internalError

  let handleNewTweet createTweet (user : User) context = async {
    match JSON.deserialize context.request  with
    | Success (PostRequest post) -> 
      match Post.TryCreate post with
      | Success post -> 
        let! webPart = 
          createTweet user.UserId post
          |> AR.either onCreateTweetSuccess onCreateTweetFailure
        return! webPart context
      | Failure err -> 
        return! JSON.badRequest err context
    | Failure err -> 
      return! JSON.badRequest err context
  }
  
  let webPart getDataContext =
    let createTweet = Persistence.createTweet getDataContext 
    choose [
      path "/wall" >=> requiresAuth renderWall
      POST >=> path "/tweets"  
        >=> requiresAuth2 (handleNewTweet createTweet)  
    ]
