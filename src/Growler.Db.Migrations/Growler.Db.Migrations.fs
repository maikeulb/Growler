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

[<Migration(201801251212L, "Creating Growl Table")>]
type CreateGrowlTable()=
  inherit Migration()

  override this.Up() =
    base.Create.Table("Growls")
      .WithColumn("Id").AsGuid().PrimaryKey()
      .WithColumn("Post").AsString(144).NotNullable()
      .WithColumn("UserId").AsInt32().ForeignKey("Users", "Id")
      .WithColumn("GrowledAt").AsDateTimeOffset().NotNullable()
    |> ignore
  
  override this.Down() = 
    base.Delete.Table("Growls") |> ignore

[<Migration(201801240554L, "Creating Social Table")>]
type CreateSocialTable()=
  inherit Migration()

  override this.Up() =
    base.Create.Table("Social")
      .WithColumn("Id").AsGuid().PrimaryKey().Identity()
      .WithColumn("FollowerUserId").AsInt32().ForeignKey("Users", "Id").NotNullable()
      .WithColumn("FollowingUserId").AsInt32().ForeignKey("Users", "Id").NotNullable()
    |> ignore
    base.Create.UniqueConstraint("SocialRelationship")
      .OnTable("Social")
      .Columns("FollowerUserId", "FollowingUserId") |> ignore
  
  override this.Down() = 
    base.Delete.Table("Social") |> ignore
