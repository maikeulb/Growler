namespace Auth

module Domain = 
  open User
  open Chessie.ErrorHandling

  type LoginRequest = {
    Username : Username
    Password : Password
  }
  with static member TryCreate (username, password) = 
        trial {
          let! username = Username.TryCreate username
          let! password = Password.TryCreate password
          return {
            Username = username
            Password = password
          }
        }


module Suave =
  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.DotLiquid
  open Suave.Form
  open Chessie.ErrorHandling
  open Chessie
  open Domain

  type LoginViewModel = {
    Username : string
    Password : string
    Error : string option
  }

  let emptyLoginViewModel = {
    Username = ""
    Password = ""
    Error = None
  }

  let handleUserLogin context = async {
    match bindEmptyForm context .request with
    | Choice1Of2 (vm : LoginViewModel) ->
      let result = 
        LoginRequest.TryCreate (vm.Username, vm.Password)
      match result with
      | Success req -> 
        return! Successful.OK "TODO" context
      | Failure err -> 
        let viewModel =
          {vm with Error = Some err}
        return! page "account/login.liquid" viewModel context
    | Choice2Of2 err ->
      let viewModel = 
        {emptyLoginViewModel with Error = Some err}
      return! page "account/login.liquid" viewModel context
  }

  let renderLoginPage viewModel = 
    page "account/login.liquid" viewModel

  let webPart () =
    path "/login" >=> choose [
      GET >=> renderLoginPage emptyLoginViewModel
      POST >=> handleUserLogin
    ]
