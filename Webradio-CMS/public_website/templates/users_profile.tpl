<div class="user_profile">
	<div class="box-header"><h3>Profil von: [#USER_NAME#]</h3></div>
	<div class="box-content">
		<div class="tr"><div class="th">Allgemeine Infos</div></div>
		<div class="tr"><div class="th">Name:</div><div class="td">[#USER_NAME#]</div></div>
		<div class="tr">
			<div class="th">Profilbild:</div>
			<div class="td">[#USER_AVATAR#]</div>
		</div>
		<div class="tr"><div class="th">ID:</div><div class="td">[#USER_ID#]</div></div>
		<div class="tr"><div class="th">Gruppe:</div><div class="td">[#USER_LEVEL#]</div></div>
	</div>
</div>

<div class="useredit_box">
	<div class="box-header">
		<h3>Profil - Bearbeitung</h3>
	</div>
	<div class="box-content">
		<form action="/providers/users/" method="POST" id="user_editprofile" name="user_editprofile" enctype="multipart/form-data">
			<input type="hidden" name="userid" id="userid" value="[#USER_ID#]" />
			<div class="box-header">
				<h3>Profil bearbeiten</h3>
			</div>
			<div class="box-content">
				<label for="username">Benutzername: </label>
				<br />
				<input type="input" name="username" id="username" class="input_text" width="100%" value="[#USER_NAME#]" />

				<br />
				<br />

				<label for="password">Password (min. 6 zeichen): </label>
				<br />
				<input type="password" name="password" id="password" class="input_text" width="100%" />
				<br />

				<label for="password_agree">Password (Bestätigung): </label>
				<br />
				<input type="password" name="password_agree" id="password_agree" class="input_text" width="100%" />
				<br />
				<br />
				[#USER_LVLSETTINGS#]

				[#USER_MODSETTINGS#]

				<input type="submit" value="speichern" onclick="return sendForm('/providers/users/','#content','.useredit-box','#user_editprofile','edit', '', '')" />
			</div>
		</form>

		<form action="/providers/users/" method="post" enctype="multipart/form-data" id="user_fileupload"
			  onsubmit="return sendForm('/providers/users/','#content','.useredit-box','#user_fileupload','edit', '', '')" >
			<input type="file" name="dateiupload" />
			<input type="submit" name="btn" />
		</form>
	</div>
</div>
