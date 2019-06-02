<div class="sp_opts" id="sp_opts_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]" style="display: none;">
	<a href="#" onclick="LoadDocument('/providers/sendeplan/','#content','Sendeplan','edit','day=[#DAY#]&hour=[#HOUR#]&ts=[#TS#]&spident=[#SPIDENT#]', '')"><img src="images/edit.png"> [bearbeiten]</a>
	<a href="#" onclick="LoadDocument('/providers/sendeplan/','#content','Sendeplan','del','day=[#DAY#]&hour=[#HOUR#]&ts=[#TS#]&spident=[#SPIDENT#]', '')"><img src="images/delete.png"> entfernen</a>
	<a href="#" onclick="toggle('#sp_opts_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]','#sp_entry_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]', '')"><img src="images/back.png"> Zurück</a></a>
</div>

<div class="sp_row" id="sp_entry_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]" onclick="toggle('#sp_entry_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]', '#sp_opts_[#SPIDENT#]_[#SPID#]_[#TS#]_[#DAY#]_[#HOUR#]')">
	<div class="sp_row_left">
		<h3>[#HOUR#]:00</h3>
	</div>
	<div class="sp_row_right"><img src="[#AVATAR#]" align="left" />
		<p class="sp_mod">Moderator: <a href="#" onclick="LoadDocument('/providers/users/','#content','Benutzerprofil','profile','uid=[#UID#]', '')">
		[#MOD#]</a></p><p class="sp_topic">Titel: [#TOPIC#]</p>
	</div>
</div>
