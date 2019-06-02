<form action="/providers/sendeplan/" method="POST" id="sp_add" 
	name="sp_add" enctype="application/x-www-form-urlencoded">
	<input type="hidden" name="day" id="day" value="[#DAY#]" />
	<input type="hidden" name="uid" id="uid" value="[#UID#]" />
	<input type="hidden" name="timestamp" id="timestamp" value="[#TS#]" />
	<input type="hidden" name="spident" id="spident" value="[#SPIDENT#]" />
	
	<label for="desc">Thema: </label>
	<textarea name="desc" id="desc" class="input_textarea">[#TOPIC#]</textarea><br />
	
	<label for="hour">Stunde: </label>
	<select name="hour" id="hour" class="input_text">
		[#SP_HOURS#]
	</select> Uhr<br />
	
	<label for="user">Moderator: </label>
	<select name="user" id="user" class="input_text">
		[#SP_MODS#]
	</select><br />

	<input type="submit" value="Eintragen" onclick="return sendForm('/providers/sendeplan/','#content', '#sp_add_[#DAY#]','#sp_add','add', '', '')" />
</form>
