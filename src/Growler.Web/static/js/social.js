$(function(){
  
  $("#follow").on('click', function(){
    var $this = $(this);
    var userId = $this.data('user-id');
    $.ajax({
      url : "/follow",
      type: "post",
      data: JSON.stringify({userId : userId}),
      contentType: "application/json"
    }).done(function(){
      message = "successfully followed";
                new Noty({
                  theme: 'relax',
                  type: 'success',
                  text: message
                }).setTimeout(3000).show();
      $this.attr('id', 'unfollow');
      $this.html('Following');
      $this.addClass('disabled');
    }).fail(function(jqXHR, textStatus, errorThrown) {
      console.log({jqXHR : jqXHR, textStatus : textStatus, errorThrown: errorThrown})
      message = "something went wrong";
                new Noty({
                  theme: 'relax',
                  type: 'alert',
                  text: message
                }).setTimeout(3000).show();
    });
  });

  var usersTemplate = `
    {{#users}}
      <div class="well user-card">
        <a href="/{{username}}">@{{username}}</a>
      </div>
    {{/users}}`;

  
  function renderUsers(data, $body, $count) {
    var htmlOutput = Mustache.render(usersTemplate, data);
    $body.html(htmlOutput);
    $count.html(data.users.length);
  }
  

  (function loadFollowers () {
    var url = "/" + growler.user.id  + "/followers"
    $.getJSON(url, function(data){
      renderUsers(data, $("#followers"), $("#followersCount"))
    })
  })();

  (function loadFollowingUsers() {
    var url = "/" + growler.user.id  + "/following"
    $.getJSON(url, function(data){
      renderUsers(data, $("#following"), $("#followingCount"))
    })
  })();

});
