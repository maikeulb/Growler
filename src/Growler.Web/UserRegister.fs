namespace UserRegister

module Suave =
  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.DotLiquid
  open Suave.Form

  type UserRegisterViewModel = {
    Username : string
    Email : string
    Password: string
    Error : string option
  }  

  let emptyUserRegisterViewModel = {
    Username = ""
    Email = ""
    Password = ""
    Error = None
  }

  let registerTemplatePath = "account/register.liquid" 

  let handleUserRegister context = async {
    match bindEmptyForm context.request with
    | Choice1Of2 (viewModel: UserRegisterViewModel) ->
      printfn "%A" viewModel
      return! Redirection.FOUND "/register" context
    | Choice2Of2 err ->
      let viewModel = {emptyUserRegisterViewModel with Error = Some err}
      return! page registerTemplatePath viewModel context
  }
  
  let webPart() =
      path "/register" 
        >=> choose [
          GET >=> page "account/register.liquid" emptyUserRegisterViewModel
          POST >=> handleUserRegister
        ]
