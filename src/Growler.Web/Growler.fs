namespace Growl
open User
open Chessie.ErrorHandling

type GrowlId = GrowlId of System.Guid

type Post = private Post of string with
  static member TryCreate (post : string) =
    match post with
    | null | ""  -> fail "Growl should not be empty"
    | x when x.Length > 140 -> fail "Growl should not be more than 140 characters"
    | x -> Post x |> ok
  member this.Value = 
    let (Post post) = this
    post

type CreateGrowl = UserId -> Post -> AsyncResult<GrowlId, System.Exception>

type Growl = {
  UserId : UserId
  Username : Username
  Id : GrowlId
  Post : Post
}

module Persistence =

  open User
  open Database
  open System

  let createGrowl (getDataContext : GetDataContext) (UserId userId) (post : Post) = asyncTrial {
    let context = getDataContext()
    let newGrowl = context.Public.Growls.Create()
    let newGrowlId = Guid.NewGuid()

    newGrowl.UserId <- userId
    newGrowl.Id <- newGrowlId
    newGrowl.Post <- post.Value
    newGrowl.GrowledAt <- DateTime.UtcNow

    do! submitUpdates context 
    return GrowlId newGrowlId
  }
