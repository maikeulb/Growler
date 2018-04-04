$(function(){
  let client = stream.connect(growler.stream.apiKey, null, growler.stream.appId);
  let userFeed = client.feed("user", growler.user.id, growler.user.feedToken);

  userFeed.get({
    limit: 25
  }).then(function(body) {
    $(body.results.reverse()).each(function(index, growl){
      renderGrowl($("#growls"), growl);
    });
  })
});
