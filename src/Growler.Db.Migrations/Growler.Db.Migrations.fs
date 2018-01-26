namespace Growler.Db.Migrations

open FluentMigrator

[<Migration(201801230622L, "Creating User Table")>]
type CreateUserTable()=
  inherit Migration()

  override this.Up() = 
    base.Create.Table("Users")
      .WithColumn("Id").AsInt32().PrimaryKey().Identity()
      .WithColumn("Username").AsString(12).Unique().NotNullable()
      .WithColumn("Email").AsString(254).Unique().NotNullable()
      .WithColumn("PasswordHash").AsString().NotNullable()
      .WithColumn("EmailVerificationCode").AsString().NotNullable()
      .WithColumn("IsEmailVerified").AsBoolean()
    |> ignore
    
  override this.Down() = 
    base.Delete.Table("Users") |> ignore

[<Migration(201801251212L, "Creating Tweet Table")>]
type CreateTweetTable()=
  inherit Migration()

  override this.Up() =
    base.Create.Table("Tweets")
      .WithColumn("Id").AsGuid().PrimaryKey()
      .WithColumn("Post").AsString(144).NotNullable()
      .WithColumn("UserId").AsInt32().ForeignKey("Users", "Id")
      .WithColumn("TweetedAt").AsDateTimeOffset().NotNullable()
    |> ignore
  
  override this.Down() = 
    base.Delete.Table("Tweets") |> ignore
