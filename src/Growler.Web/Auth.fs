namespace Auth

module Suave =
  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.DotLiquid
  open Suave.Form
  open Chessie.ErrorHandling
  open Chessie

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

  let renderLoginPage viewModel = 
    page "account/login.liquid" viewModel

  let webPart () =
    path "/login" >=> choose [
      GET >=> renderLoginPage emptyLoginViewModel
    ]
