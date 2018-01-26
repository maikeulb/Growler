namespace Social

module Domain = 
  open System
  open Chessie.ErrorHandling
  open User

  type CreateFollowing = User -> UserId -> AsyncResult<unit, Exception>
  type Subscribe = User -> UserId -> AsyncResult<unit, Exception>
  type FollowUser = User -> UserId -> AsyncResult<unit, Exception>

  let followUser 
    (subscribe : Subscribe) (createFollowing : CreateFollowing) 
    user userId = asyncTrial {

    do! subscribe user userId
    do! createFollowing user userId

  } 

module Persistence =
  open Database
  open User
  open Chessie.ErrorHandling
  open FSharp.Data.Sql
  open Chessie

  let createFollowing (getDataContext : GetDataContext) (user : User) (UserId userId) = 
     
     let context = getDataContext ()
     let social = context.Public.Social.Create()
     let (UserId followerUserId) = user.UserId
      
     social.FollowerUserId <- followerUserId
     social.FollowingUserId <- userId

     submitUpdates context

module GetStream = 
  open User
  open Chessie

  let subscribe (getStreamClient : GetStream.Client) (user : User) (UserId userId) = 
    let (UserId followerUserId) = user.UserId
    let timelineFeed = 
      GetStream.timeLineFeed getStreamClient followerUserId
    let userFeed =
      GetStream.userFeed getStreamClient userId
    timelineFeed.FollowFeed(userFeed) 
    |> Async.AwaitTask
    |> AR.catch


