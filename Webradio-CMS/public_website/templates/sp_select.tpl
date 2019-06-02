<div class="sendeplanlist-box">
	<div class="box-header">
		<h3>Sendeplan - Auswahl</h3>
	</div>
	<div class="box-content">
		<form action="/providers/sendeplan/" method="POST" id="sp_select" name="sp_select" enctype="application/x-www-form-urlencoded">
			<label for="hour">Sendeplan: </label>
			<select name="spident" id="spident" class="input_text">
				[#SPIDENT_LIST#]
			</select>

			<input type="submit" value="Weiter" onclick="return sendForm('/providers/sendeplan/','#content', '','#sp_select','show', '', '')" />
		</form>
	</div>
</div>
<br />
