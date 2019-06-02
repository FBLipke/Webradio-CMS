<form action="/providers/newsticker/" method="POST" id="sp_add" 
	name="sp_add" enctype="application/x-www-form-urlencoded">
	<input type="hidden" name="userid" id="uid" value="[#USERID#]" />

	
	<label for="desc">Titel: </label>
	<textarea name="desc" id="desc" class="input_textarea">[#TOPIC#]</textarea><br />
	<label for="desc">Titel: </label>
	
	<input name="title" type="text" id="newstitle" class="input_textarea" value="News Title...." /><br />
	<!-- <input type="submit" value="Eintragen" onclick="return sendForm('/providers/newsticker/','#content', '#sp_add_[#DAY#]','#sp_add','add', '', '')" /> -->
</form>