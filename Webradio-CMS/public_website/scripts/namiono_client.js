function Create_ContentBox(frame, provider, content, action = 'show') {
	var _jsondata = JSON.parse(content);

	var output = "";
	output += "<div class=\"" + provider.toLowerCase() + "-box\" align=\"center\" width=\"40%\">\n";
	output += "<div class=\"box-header\">" + provider + "</div>\n";
	output += "<div class=\"box-content\">\n";

	if (_jsondata.length > 0)
	{
		if (provider == "Navigation" || provider == "Users") {
			output += "<ul>\n";
		}

		for (var i = 0; i < _jsondata.length; i++)
		{
			if (provider == 'News') {
				output += "<div class=\"" + provider + "-box\" align=\"center\" width=\"40%\">\n";
				output += "<div class=\"box-header\">" + _jsondata[i].Name + "</div>\n";
				output += "<div class=\"box-content\">" + _jsondata[i].OutPut + '</div>\n';

				output += "<div class=\"box-content\">Erstellt von: <a href=\"#\" onclick=\"Request('Users', '#content', 'profile', 'Id=" + _jsondata[i].Author + "', 'GET');\">"
					+ Get_ExtraData_Replace(_jsondata[i].ExtraData, _jsondata[i].Author) + "</a> am " + GetDateFromTimestamp(_jsondata[i].Updated) + "</div>\n";
				output += "</div>\n";
			}

			if (provider == 'Navigation')
			{
				output += "<li>" + MakeNavigationLink(_jsondata[i]) + "</li>\n";
			}

			if (provider == 'Users')
			{
				output += "<li class=\"ul_row\">" + MakeToggleEntry(_jsondata[i]) + "</li>\n";
			}
		}


		if (provider == 'Navigation' || provider == 'Users') {
			output += "</ul>\n";
		}
	}

	if (provider == "Users")
	{
		if (_jsondata.FormatType == '0') {
			
			if (action == 'login')
			{
				if (AccessCookie("User") == null)
				{
					output += "<li><span id=\"UserLogin-Nav\"></span></li>\n";
					Request('Navigation', '#UserLogin-Nav', 'show', '', 'GET');
				}
				else
				{
					output += "<li>" + _jsondata.Name + "</li>\n";
					output += "<li><img src=\"" + _jsondata.Image + "\" width=\"" + _jsondata.Width + "\" height=\"" + _jsondata.Height + "\"></li>\n";
				}
			}
			else
			{
				if (action != 'profile')
				{
					output += "<li><span id=\"UserInfo-Nav\"></span></li>\n";
					Request('Navigation', '#UserInfo-Nav', 'show', '', 'GET');
				}
			}
		}
		
		if (_jsondata.Controls != null)
		{
			if (_jsondata.ControlType == '6')
			{
				var _form = '<form name="' + _jsondata.Name + '" id="' + _jsondata.Id +
				'" method="' + _jsondata.Method + '" action="' + _jsondata.Url + '">\n';

				_form += GenerateFormular(_jsondata.Id, _jsondata.Controls);
				_form += '</form>\n';
				output += _form;
			}
		}
	}


	output += "</div></div>\n";
	
	$(frame).html(output);
	console.log($(document));
}

function GetCurrentUser() {
	
	if (AccessCookie("User") != null)
	{
		Request('Users', '#userinfo', 'login', 'Id=' + AccessCookie("User").trim(), 'GET');
	}
	else
	{
		Request("Users", "#userinfo", "login",'','GET');
	}
}

function GenerateFormular(id, form) {

	var _form = "";

	for (var ic = 0; ic < form.length; ic++) {
		var control = form[ic];

		if (!control.Enabled) {
			continue;
		}

		if (control.Type == '0') {
			_form += '<input type="input" name="' + control.Name
				+ '" id="' + control.Id + '" value="" />\n';
		}

		if (control.Type == '5') {
			_form += '<input type="password" name="' + control.Name
				+ '" id="' + control.Id + '" value="" />\n';
		}

		if (control.Type == '2') {
			_form += '<input type="submit" name="' + control.Name
				+ '" id="' + control.Id + '" value="Absenden" onclick="return SendForm(\'#' + id + '\')" />\n';
		}

		if (control.Type == '3') {
			_form += '<input type="hidden" name="' + control.Name
				+ '" id="' + control.Id + '" value="' + control.Value + '" />\n';
		}

		if (control.Type == '7') {
			_form += '<label for="' + control.Id + '">' + control.Value + '</label>\n';
		}
	}

	return _form;
}

function Request(provider, frame, action, parameters, method = "") {
	var _method = method;
	var _accepts = "Application/json";

	if (method == "") {
		_method = "GET";
	}
	else {
		_method = "POST";
	}

	var request = $.ajax({
		url: '/providers/' + provider.toLowerCase() + '/',
		method: _method,
		cache: false,
		async: true,
		dataType: "json",
		data: JSON.stringify(parameters),
		accepts: _accepts,
		beforeSend: function (request) {
			$(frame).fadeOut(50, function () {
				
			});
			request.setRequestHeader("Authorization", "Basic " + btoa("Namiono_new:[#XDMP_AUTH#]"));
			request.setRequestHeader("UAgent", "Namiono_new");
			request.setRequestHeader("Action", action);
			request.setRequestHeader("Provider", provider);
			request.setRequestHeader("Frame", frame);
		},
		success: function (response) {
			$(frame).html = "";
		},
		error: function (response, status, text) {
			$(frame).fadeIn(50, function () {
				$(frame).html(response.status + " : " + text);
			});
		},
		complete: function (response) {

			$(frame).fadeIn(50, function () {
				if (response.getResponseHeader("Content-Type") == _accepts) {
					Create_ContentBox(frame, provider, response.responseText, response.getResponseHeader("Action"));
				}
				else {
					$(frame).html = response.responseText;
				}
			});
		}
	});

	return false;
}

function SendForm(form) {
	$(form).submit(function (evt) {
		evt.preventDefault();

		var _prov = $(form).find('input[name="Provider"]').val();
		var _action = $(form).find('input[name="Action"]').val();
		var _frame = $(form).find('input[name="Frame"]').val();

		var req = $.ajax({
			url: '/providers/' + _prov + '/',
			method: "POST",
			cache: false,
			async: true,
			data: $(form).serialize(),
			accepts: "Application/json",
			beforeSend: function (request) {
				$(_frame).fadeOut(50, function () {
					$(_frame).html = "";
				});
				request.setRequestHeader("Authorization", "Basic " + btoa("Namiono_new:[#XDMP_AUTH#]"));
				request.setRequestHeader("UAgent", "Namiono_new");
				request.setRequestHeader("Action", _action);
				request.setRequestHeader("Provider", _prov);
				request.setRequestHeader("Frame", _frame);
			},
			success: function (response) {
			},
			error: function (response, text) {
				$(_frame).fadeIn(50, function () {
					$(_frame).html(response.status + " : " + text);
				});
			},
			complete: function (response) {
				$(_frame).fadeIn(50, function () {

					if (response.status == 308)
					{
						document.location.href = response.getResponseHeader("Location");
						console.log("Redirected to: " + response.getResponseHeader("Location"));
					}
					else
					{
						if (response.getResponseHeader("Content-Type") == "Application/json") {
							Create_ContentBox(_frame, _prov, response.responseText, response.getResponseHeader("Action"));
						}
						else {
							$(_frame).html = response.responseText;
						}					
					}
				});
			}
		});

		return false;
	});
}

function GetDateFromTimestamp(timestamp) {
	var date = new Date(timestamp * 1000)
	return date.getDate() + '.' + (date.getMonth() + 1) + '.' + date.getFullYear();
}

function AccessCookie(cookieName)
{
	var name = cookieName + "=";
	var allCookieArray = document.cookie.split(';');

	for (var i = 0; i < allCookieArray.length; i++)
	{
		var temp = allCookieArray[i].trim();
		if (temp.indexOf(name) == 0)
			return temp.substring(name.length, temp.length);
	}

	return null;
}

function MakeNavigationLink(entry)
{
	var _data = null
	
	if (entry.Url != null)
	{
		_data = entry.Url.split(';');
	}
	else
	{
		if (entry.OutPut == null)
			_data = "";
		else
			_data = entry.OutPut.split(';');
	}

	if (_data[0] == 'direct')
	{
		if (entry.Image != null)
		{
			return "<a href=\"" + _data[1].trim() + " \">" + entry.Name + "</a>\n";
		}
		else
		{
			return "<img src=\"" + entry.Image + "\" width=\"" + entry.Width + "\" height=\"" + entry.Height + "\" /> <a href=\"" + _data[1].trim() + "\">" + entry.Name + "</a>\n";
		}
	}
	else
	{
		if (entry.Image != null)
		{
			return "<a href=\"#\" onclick=\"Request('" + entry.Provider + "', '" + entry.Frame + "', '" + entry.Action + "', '', 'GET');\">" + entry.Name + "</a>\n";
		}
		else
		{
			return "<img src=\"" + entry.Image + "\" width=\"" + entry.Width + "\" height=\"" + entry.Height + "\" />\n<a href=\"#\" onclick=\"Request('" +	entry.Provider + "', '" + entry.Frame + "', '" + entry.Action + "', '', 'GET');\">" + entry.Name + "</a>\n";
		}
	}
}

function MakeToggleEntry(data)
{
	var output = '';

	output += "<div onclick=\"\toggle('#sp_entry_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]', '#sp_opts_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]')\">\n";

	var _image = "";
	
	output += "<div class=\"ul_row_left\"><img src=\"" + data.Image + "\" Width=\"" + data.Width + "\" height=\"" + data.Height + "\" /></div>\n";
	output += "<div class=\"ul_row_right\">" + data.Name + " (" + data.ExtraData + ")</div>\n";
	output += "</div>\n";

	return output;
}

function Replace(text, oldValue, newValue)
{
	return text.replace(oldValue, newValue);
}

function Get_ExtraData_Replace(data, text)
{
	var _data = data.split(';');

	for (var ic = 0; ic < data.length; ic++)
	{
		if (_data[ic] == 'replace')
		{
			var paterns = _data[(ic + 1)].split('=>');

			text = Replace(text, paterns[0], paterns[1]);
		}
	}

	return text;
}