$(function() {
  $("#growlForm").submit(function(event) {
    var $this = $(this);
    var $growl = $("#growl");
    event.preventDefault();
    $this.prop('disabled', true);
    $.ajax({
      url: "/growls",
      type: "post",
      data: JSON.stringify({
        post: $growl.val()
      }),
      contentType: "application/json"
    }).done(function() {
      $this.prop('disabled', false);
      message = "successfully posted";
      new Noty({
        theme: 'relax',
        type: 'success',
        text: message
      }).setTimeout(3000).show();
      $growl.val('');
    }).fail(function(jqXHR, textStatus, errorThrown) {
      console.log({
        jqXHR: jqXHR,
        textStatus: textStatus,
        errorThrown: errorThrown
      })
      message = "something went wrong";
      new Noty({
        theme: 'relax',
        type: 'error',
        text: message
      }).setTimeout(3000).show();
    });

  });

  $("textarea[maxlength]").on("propertychange input", function() {
    if (this.value.length > this.maxlength) {
      this.value = this.value.substring(0, this.maxlength);
    }
  });

  let client = stream.connect(growler.stream.apiKey, null, growler.stream.appId);
  let userFeed = client.feed("user", growler.user.id, growler.user.feedToken);
  let timelineFeed = client.feed("timeline", growler.user.id, growler.user
    .timelineToken);

  userFeed.subscribe(function(data) {
    renderGrowl($("#wall"), data.new[0]);
  });
  timelineFeed.subscribe(function(data) {
    renderGrowl($("#wall"), data.new[0]);
  });

  timelineFeed.get({
    limit: 25
  }).then(function(body) {
    var timelineGrowls = body.results
    userFeed.get({
      limit: 25
    }).then(function(body) {
      var userGrowls = body.results
      var allGrowls = $.merge(timelineGrowls, userGrowls)
      allGrowls.sort(function(t1, t2) {
        return new Date(t2.time) - new Date(t1.time);
      })
      $(allGrowls.reverse()).each(function(index, growl) {
        renderGrowl($("#wall"), growl);
      });
    })
  })
});
