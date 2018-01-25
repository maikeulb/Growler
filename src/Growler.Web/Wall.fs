namespace Wall

module Suave =
  open Suave
  open Suave.Filters
  open Suave.Operators
  open User
  open Auth.Suave

  let renderWall (user : User) context = async {
    return! Successful.OK user.Username.Value context
  }
  
  let webpart () =
    path "/wall" >=> requiresAuth renderWall
