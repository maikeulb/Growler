Growler
=======

Twitter clone where users share growls (tweets) with other users. Project made
with reference to FsTweet (from Fsharp Applied II).

Technology
----------
* Suave
* PostgreSQL (using SQLProvider)
* FluentMigrator
* WebPack
* UIKit

Screenshots
---------
### Main
![main](/screenshots/main.png?raw=true "Post")

### Wall
![wall](/screenshots/wall.png?raw=true "Post")

### Profile
![profile](/screenshots/profile.png?raw=true "Post")

Run
---
You will need Mono, Forge, Fake, and a stream account (it's free, you can sign up
[here]("https://getstream.io/") ).
If you meet those requirements, then: create a database, open `build.fsx` and point the database URI to your server,
open `Growler.Web.fs` and set your Api keys.

After that has been taken care of,

```
npm install
forge fake build (to ensure project compiles correctly)
npm run start (runs webpack dev server and suave server simultaneously)
Go to http://localhost:8080
```

TODO
----
Dockerfile  
Add unfollowing feature  
Add trending feature  
Add like feature
