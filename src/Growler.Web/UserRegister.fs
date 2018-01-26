namespace UserRegister

module Domain =
  open Chessie.ErrorHandling
  open BCrypt.Net
  open System.Security.Cryptography
  open Chessie
  open User

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

  let base64URLEncoding bytes =
    let base64String = 
       System.Convert.ToBase64String bytes
    base64String.TrimEnd([|'='|])
      .Replace('+', '-').Replace('/', '_')

  type VerificationCode = private VerificationCode of string with
    member this.Value =
      let (VerificationCode verificationCode) = this
      verificationCode
    static member Create () =
      use rngCsp = new RNGCryptoServiceProvider()
      let verificationCodeLength = 15
      let b : byte [] = 
        Array.zeroCreate verificationCodeLength
      rngCsp.GetBytes(b)
      base64URLEncoding b
      |> VerificationCode 

  type CreateUserRequest = {
    Username : Username
    PasswordHash : PasswordHash
    Email : EmailAddress
    VerificationCode : VerificationCode
  }

  type CreateUserError =
  | EmailAlreadyExists
  | UsernameAlreadyExists
  | Error of System.Exception

  type CreateUser = 
    CreateUserRequest -> AsyncResult<UserId, CreateUserError>

  type RegisterEmailRequest = {
    Username : Username
    EmailAddress : EmailAddress
    VerificationCode : VerificationCode
  }

  type SendEmailError = SendEmailError of System.Exception

  type SendRegisterEmail = RegisterEmailRequest -> AsyncResult<unit, SendEmailError>
  
  type UserRegisterError =
  | CreateUserError of CreateUserError
  | SendEmailError of SendEmailError

  type RegisterUser = 
    CreateUser -> SendRegisterEmail -> UserRegisterRequest 
      -> AsyncResult<UserId, UserRegisterError>

  let registerUser (createUser : CreateUser) 
                 (sendEmail : SendRegisterEmail) 
                 (req : UserRegisterRequest) = asyncTrial {

    let createUserReq = {
      PasswordHash = PasswordHash.Create req.Password
      Username = req.Username
      Email = req.EmailAddress
      VerificationCode = VerificationCode.Create()
    }

    let! userId = 
      createUser createUserReq
      |> AR.mapFailure CreateUserError

    let sendEmailReq = {
      Username = req.Username
      VerificationCode = createUserReq.VerificationCode
      EmailAddress = createUserReq.Email
    }
    do! sendEmail sendEmailReq 
        |> AR.mapFailure SendEmailError

    return userId
  }

module Persistence =
  open Domain
  open Chessie.ErrorHandling
  open Database
  open System
  open Chessie
  open User

  let private mapException (ex : System.Exception) =
    match ex with
    | UniqueViolation "IX_Users_Email" _ ->
      EmailAlreadyExists
    | UniqueViolation "IX_Users_Username" _ -> 
      UsernameAlreadyExists
    | _ -> Error ex

  let createUser (getDataContext : GetDataContext) createUserReq = asyncTrial {
    let context = getDataContext ()
    let users = context.Public.Users

    let newUser = users.Create()
    newUser.Email <- createUserReq.Email.Value
    newUser.EmailVerificationCode <- 
      createUserReq.VerificationCode.Value
    newUser.Username <- createUserReq.Username.Value
    newUser.IsEmailVerified <- true
    newUser.PasswordHash <- createUserReq.PasswordHash.Value

    do! submitUpdates context
        |> AR.mapFailure mapException

    printfn "User Created %A" newUser.Id
    return UserId newUser.Id
  }
    
module Email =
  open Domain
  open Chessie.ErrorHandling

  let sendRegisterEmail registerEmailReq = asyncTrial {
    printfn "Email %A sent" registerEmailReq
    return ()
  }
      
module Suave =
  open Domain
  open Chessie.ErrorHandling
  open Suave
  open Suave.Filters
  open Suave.Operators
  open Suave.DotLiquid
  open Suave.Form
  open Database
  open Chessie
  open User

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

  let accountTemplatePath = "account/register.liquid" 

  let handleCreateUserError viewModel = function 
  | EmailAlreadyExists ->
    let viewModel = 
      {viewModel with Error = Some ("email already exists")}
    page accountTemplatePath viewModel
  | UsernameAlreadyExists ->
    let viewModel = 
      {viewModel with Error = Some ("username already exists")}
    page accountTemplatePath viewModel
  | Error ex ->
    printfn "Server Error : %A" ex
    let viewModel = 
      {viewModel with Error = Some ("something went wrong")}
    page accountTemplatePath viewModel

  let handleSendEmailError viewModel err =
    printfn "error while sending email : %A" err
    let viewModel = 
      {viewModel with Error = Some ("something went wrong")}
    page accountTemplatePath viewModel

  let onUserRegisterFailure viewModel err = 
    match err with
    | CreateUserError cuErr ->
      handleCreateUserError viewModel cuErr
    | SendEmailError err ->
      handleSendEmailError viewModel err

  let onUserRegisterSuccess viewModel _ =
    sprintf "/register/success/%s" viewModel.Username
    |> Redirection.FOUND 

  let handleUserRegisterResult viewModel result =
    either 
      (onUserRegisterSuccess viewModel)
      (onUserRegisterFailure viewModel) result

  let handleUserRegisterAsyncResult viewModel aResult = 
    aResult
    |> Async.ofAsyncResult
    |> Async.map (handleUserRegisterResult viewModel)

  let handleUserRegister registerUser context = async {
    match bindEmptyForm context.request with
    | Choice1Of2 (vm : UserRegisterViewModel) ->
      let result =
        UserRegisterRequest.TryCreate (vm.Username, vm.Password, vm.Email)
      match result with
      | Success userRegisterReq ->
        let userRegisterAsyncResult = registerUser userRegisterReq
        let! webPart =
          handleUserRegisterAsyncResult vm userRegisterAsyncResult
        return! webPart context
      | Failure msg ->
        let viewModel = {vm with Error = Some msg}
        return! page accountTemplatePath viewModel context
    | Choice2Of2 err ->
      let viewModel = {emptyUserRegisterViewModel with Error = Some err}
      return! page accountTemplatePath viewModel context
  }

  let webPart getDataContext =
    let createUser = Persistence.createUser getDataContext
    let sendRegisterEmail = Email.sendRegisterEmail
    let registerUser = Domain.registerUser createUser sendRegisterEmail
    choose [
      path "/register" 
        >=> choose [
          GET >=> page accountTemplatePath emptyUserRegisterViewModel
          POST >=> handleUserRegister registerUser
        ]
      pathScan "/register/success/%s" (page "account/register_success.liquid")
    ]
