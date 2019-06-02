<div class="speditbox-box">
	<div class="box-header">
		<h3>Sendung bearbeiten</h3>
	</div>
	<div class="box-content">
	<form action="/providers/sendeplan/" method="POST" id="sp_edit" name="sp_edit" enctype="application/x-www-form-urlencoded">
	<input type="hidden" name="day" id="day" value="[#DAY#]" />
	<input type="hidden" name="hour" id="hour" value="[#HOUR#]" />
	<input type="hidden" name="ts" id="ts" value="[#TS#]" />
	<input type="hidden" name="spident" id="spident" value="[#SPIDENT#]" />
	
	<label for="desc">Thema: </label>
	<textarea name="desc" id="desc" class="input_textarea">[#TOPIC#]</textarea><br />

	<label for="user">Moderator: </label>
	<select name="user" id="user" class="input_text">
		[#SP_MODS#]
	</select><br />

	<input type="submit" value="Speichern" onclick="return sendForm('/providers/sendeplan/','#content', '#sp_edit_[#DAY#]','#sp_edit','edit', '', '')" />
</form>
	</div>
</div>
