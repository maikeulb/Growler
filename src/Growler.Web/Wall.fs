namespace Wall

module Domain = 
  open User
  open Growl
  open Chessie.ErrorHandling
  open System
  open Chessie

  type NotifyGrowl = Growl -> AsyncResult<unit, Exception>

  type PublishGrowlError =
  | CreateGrowlError of Exception
  | NotifyGrowlError of (GrowlId * Exception)

  type PublishGrowl =
    CreateGrowl -> NotifyGrowl -> 
      User -> Post -> AsyncResult<GrowlId, PublishGrowlError>

  let publishGrowl createGrowl notifyGrowl (user : User) post = asyncTrial {
    let! growlId = 
      createGrowl user.UserId post
      |> AR.mapFailure CreateGrowlError

    let growl = {
      Id = growlId
      UserId = user.UserId
      Username = user.Username
      Post = post
    }
    do! notifyGrowl growl 
        |> AR.mapFailure (fun ex -> NotifyGrowlError(growlId, ex))

    return growlId
  }

module GetStream = 
  open Growl
  open User
  open Stream
  open Chessie.ErrorHandling

  let mapStreamResponse response =
    match response with
    | Choice1Of2 _ -> ok ()
    | Choice2Of2 ex -> fail ex
  let notifyGrowl (getStreamClient: GetStream.Client) (growl : Growl) = 
    
    let (UserId userId) = growl.UserId
    let (GrowlId growlId) = growl.Id
    let userFeed =
      GetStream.userFeed getStreamClient userId
    
    let activity = new Activity(userId.ToString(), "growl", growlId.ToString())
    activity.SetData("growl", growl.Post.Value)
    activity.SetData("username", growl.Username.Value)
    
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
  open Growl
  open Chiron
  open Chessie.ErrorHandling
  open Chessie
  open Domain

  type WallViewModel = {
    Username :  string
    UserId : int
    UserFeedToken : string
    TimelineToken : string
    ApiKey : string
    AppId : string
  }

  type PostRequest = PostRequest of string with
    static member FromJson (_ : PostRequest) = json {
      let! post = Json.read "post"
      return PostRequest post 
    }

  let renderWall 
    (getStreamClient : GetStream.Client) 
    (user : User) context  = async {

    let (UserId userId) = user.UserId
    
    let userFeed = 
      GetStream.userFeed getStreamClient userId
    
    let timeLineFeed =
      GetStream.timeLineFeed getStreamClient userId 
    
    let vm = {
      Username = user.Username.Value 
      UserId = userId
      UserFeedToken = userFeed.ReadOnlyToken
      TimelineToken = timeLineFeed.ReadOnlyToken
      ApiKey = getStreamClient.Config.ApiKey
      AppId = getStreamClient.Config.AppId}

    return! page "main/wall.liquid" vm context 

  }

  let onPublishGrowlSuccess (GrowlId id) = 
    ["id", String (id.ToString())]
    |> Map.ofList
    |> Object
    |> JSON.ok

  let onPublishGrowlFailure (err : PublishGrowlError) =
    match err with
    | NotifyGrowlError (growlId, ex) ->
      printfn "%A" ex
      onPublishGrowlSuccess growlId
    | CreateGrowlError ex ->
      printfn "%A" ex
      JSON.internalError

  let handleNewGrowl publishGrowl (user : User) context = async {
    match JSON.deserialize context.request  with
    | Success (PostRequest post) -> 
      match Post.TryCreate post with
      | Success post -> 
        let! webPart = 
          publishGrowl user post
          |> AR.either onPublishGrowlSuccess onPublishGrowlFailure
        return! webPart context
      | Failure err -> 
        return! JSON.badRequest err context
    | Failure err -> 
      return! JSON.badRequest err context
  }
  
  let webPart getDataContext getStreamClient =
    let createGrowl = Persistence.createGrowl getDataContext 
    let notifyGrowl = GetStream.notifyGrowl getStreamClient
    let publishGrowl = publishGrowl createGrowl notifyGrowl
    choose [
      path "/wall" >=> requiresAuth (renderWall getStreamClient)
      POST >=> path "/growls"  
        >=> requiresAuth2 (handleNewGrowl publishGrowl)  
    ]
