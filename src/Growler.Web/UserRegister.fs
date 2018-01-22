namespace UserRegister

module Domain =
  open Chessie.ErrorHandling

  type EmailAddress = private EmailAddress of string with
    member this.Value =
      let (EmailAddress emailAddress) = this
      emailAddress
    static member TryCreate (emailAddress : string) =
     try 
       new System.Net.Mail.MailAddress(emailAddress) |> ignore
       EmailAddress emailAddress |> ok
     with
       | _ -> fail "Invalid Email Address"

  type Username = private Username of string with
    member this.Value =
      let (Username username) = this
      username
    static member TryCreate (username : string) =
      match username with
      | null | "" -> fail "Username should not be empty"
      | x when x.Length > 12 ->
        fail "Username should not be more than 12 characters"
      | x -> Username x |> ok

  type Password = private Password of string with
    member this.Value =
      let (Password password) = this
      password
    static member TryCreate (password : string) =
      match password with
      | null | "" -> fail "Password should not be empty"
      | x when x.Length < 4 || x.Length > 8 ->
        fail "Password should contain 4-8 characters"
      | x -> Password x |> ok

  type UserRegisterRequest = {
    Username : Username
    Password : Password
    EmailAddress : EmailAddress
  }
  with static member TryCreate (username, password, email) =
        trial {
          let! username = Username.TryCreate username
          let! password = Password.TryCreate password
          let! emailAddress = EmailAddress.TryCreate email
          return {
            Username = username
            Password = password
            EmailAddress = emailAddress
          }
        }


module Suave =
  open Domain
  open Chessie.ErrorHandling
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
      let result =
         UserRegisterRequest.TryCreate (viewModel.Username, viewModel.Password, viewModel.Email)
      let onSuccess (userRegisterRequest, _) =
        printfn "%A" userRegisterRequest
        Redirection.FOUND "/register" context
      let onFailure messages =
        let viewModel = {viewModel with Error = Some (List.head messages)}
        page registerTemplatePath viewModel context
      return! either onSuccess onFailure result
    | Choice2Of2 err ->
      let viewModel = {emptyUserRegisterViewModel with Error = Some err}
      return! page registerTemplatePath viewModel context
  }

  let webPart() =
      path "/register" 
        >=> choose [
          GET >=> page registerTemplatePath emptyUserRegisterViewModel
          POST >=> handleUserRegister
        ]
