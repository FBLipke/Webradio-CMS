<div class="userregister-box">
	<div class="box-header">
		<h3>Benutzer-Registrierung</h3>
	</div>
	<div class="box-content">
		<form action="/providers/users/" method="POST" id="user_register" name="user_register" enctype="application/x-www-form-urlencoded">
			<div class="box-header">
				<h3>Benutzerkonto</h3>
			</div>
			<div class="box-content">
				<label for="username">Benutzername: </label>
				<br />
				<input type="input" name="username" id="username" class="input_text" width="100%" />

				<br />
				<br />

				<label for="password">Password: </label>
				<br />
				<input type="password" name="password" id="password" class="input_text" width="100%" />
				<br />
				<br />

				<label for="password_agree">Password (bestätigung): </label>
				<br />
				<input type="password" name="password_agree" id="password_agree" class="input_text" width="100%" />
			</div>

			<input type="submit" value="Registrieren" onclick="return sendForm('/providers/users/','#content','.userregister-box','#user_register','add', '', '')" />
		</form>
	</div>
</div>