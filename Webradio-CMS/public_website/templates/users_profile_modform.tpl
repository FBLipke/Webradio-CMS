<form action="/providers/users/" method="POST" id="user_editprofile" name="user_editprofile" enctype="application/x-www-form-urlencoded">
	<input type="hidden" name="userid" id="userid" value="[#USER_ID#]" />
	<div class="box-header">
		<h3>Modetrator - Bearbeitung</h3>
	</div>

	<div class="box-content">
		<label for="is">Moderator</label>
		<input type="checkbox" name="ismod" id="ismod" class="input_text" [#ismod#] />
	</div>

	<input type="submit" value="speichern" onclick="return sendForm('/providers/users/','#content','.useredit-box','#user_editprofile','edit', '', '')" />
</form>