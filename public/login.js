function get_cookie(name) {
  var value = "; " + document.cookie;
  var parts = value.split("; " + name + "=");
  if (parts.length == 2) return parts.pop().split(";").shift();
}

var auth_state = function () {
  var $auth_state = $('#auth_state');
  if (typeof get_cookie('suave_session_id') === 'undefined') {
    $auth_state.text('Not yet authenticated :(');
    $("#login").show();
  } 
  else {
    $auth_state.text('You are authenticated!');
    $("#login").hide();
  }
};

var api_state = function() {
  var $api_state = $('#api_state');
  var token = localStorage.api_token
  if (typeof token === 'undefined') {
    $api_state.text('no api token');
  } 
  else {
    $api_state.text('api token');
  }
};

var delete_cookie = function(){
  document.cookie = 'suave_session_id=; expires=Thu, 01 Jan 1970 00:00:01 GMT;';
  location.reload();
};

var submit_form = function(evt){
  var usr = $('#username').val();
  var pw = $('#password').val();
  var url = 'http://localhost:8083/api/secret'

  var credentials = {
    id: 'dh37fgj492je',
    key: 'werxhqb98rpaxn39848xrunpaw3489ruxnpa98w4rxn',
    algorithm: 'sha256',
    user: usr
  };

  var credentialsFunc = function (id, callback) {
    return callback(null, credentials);
  };

  var header = hawk.client.header(url, "POST", { credentials: credentials });

  $.ajax({
    type: "POST",
    url: url,
    dataType: "json",
    headers: { authorization: header.field }
  }) 
  .done(function (response) {
    console.log("done");
    console.log(response);
    //var isValid = hawk.client.authenticate(body, credentials, header.artifacts, {
    //  payload: body.responseText,
    //  required: true
    //});

    //if (isValid) {
    //  $("#response").html(body.responseText);
    //  alert(body.responseText);
    //}
    //else {
    //  $("#response").html("not valid");
    //  alert(body.responseText);
    //}
  })
  .fail(function(jqXHR, textStatus) {
    console.log("error");
    console.log(jqXHR.status);
  });
  return false;
};

