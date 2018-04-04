$(function(){
  var timeAgo = function () {
    return function(val, render) {
      return moment(render(val) + "Z").fromNow()
    };
  }

  var template = `
    <div class="growl_read_view bg-info">
      <span class="text-muted">@{{growl.username}} - {{#timeAgo}}{{growl.time}}{{/timeAgo}}</span>
      <p>{{growl.growl}}</p>
    </div>
  `

  window.renderGrowl = function($parent, growl) {
    var htmlOutput = Mustache.render(template, {
        "growl" : growl,
        "timeAgo" : timeAgo
    });
    $parent.prepend(htmlOutput);
  };

});
