module Database

open FSharp.Data.Sql
open Chessie.ErrorHandling
open System
open Npgsql

[<Literal>]
let private connString = 
  "Server=172.17.0.2;Port=5432;Database=growler;User Id=postgres;Password=P@ssw0rd!;"

[<Literal>]
let private npgsqlLibPath = @"./../../packages/database/Npgsql/lib/net451"

[<Literal>]
let private dbVendor = Common.DatabaseProviderTypes.POSTGRESQL

type Db = SqlDataProvider<
            ConnectionString=connString,
            DatabaseVendor=dbVendor,
            ResolutionPath=npgsqlLibPath,
            UseOptionTypes=true>

type DataContext = Db.dataContext

type GetDataContext = unit -> DataContext
let dataContext (connString : string) : GetDataContext =
  let isMono = 
    System.Type.GetType ("Mono.Runtime") <> null
  match isMono with
  | true -> 
    let opts : Transactions.TransactionOptions = {
      IsolationLevel = Transactions.IsolationLevel.DontCreateTransaction
      Timeout = System.TimeSpan.MaxValue
    } 
    fun _ -> Db.GetDataContext(connString, opts)
  | _ -> 
    fun _ -> Db.GetDataContext connString


let submitUpdates (context: DataContext) = 
  context.SubmitUpdatesAsync()
  |> Async.Catch
  |> Async.map ofChoice
  |> AR

let (|UniqueViolation|_|) constraintName (ex : Exception) =
  match ex with
  | :? AggregateException as agEx  ->
    match agEx.Flatten().InnerException with 
    | :? PostgresException as pgEx ->
      if pgEx.ConstraintName = constraintName && 
          pgEx.SqlState = "23505" then
        Some ()
      else None
    | _ -> None
  | _ -> None
