<!DOCTYPE html>
<html>
	<head>
		<meta charset="utf-8">
		<meta http-equiv="X-UA-Compatible" content="IE=edge">
		<meta name="viewport" content="width=device-width, initial-scale=1.0">
		<meta http-equiv="Content-Type" content="text/html; charset=utf-8" />
		
		<title>[#SITE_TITLE#]</title>
		
		<link rel="stylesheet" type="text/css" href="styles/[#DESIGN#]/layout.css">
		<link rel="stylesheet" type="text/css" href="styles/[#DESIGN#]/style.css">
		
		<script type="text/javascript" src="scripts/jquery.js"></script>
		<script type="text/javascript" src="scripts/Namiono_Client.js"></script>

		<!--[if lt IE 9]>
		<script type="text/javascript">
			document.createElement('header');
			document.createElement('nav');
			document.createElement('aside');
			document.createElement('section');
			document.createElement('main');
		</script>
		<![endif]-->
	</head>
	<body>
		<div id="page">
			<nav>
				<section id="navigation">
					<script type="text/javascript">
						$(document).ready(function()
						{
							Request("Navigation", "#navigation", "show", "");
						});
					</script>
				</section>
			</nav>
			<main>
				<section id="content">
					<script type="text/javascript">
						$(document).ready(function()
						{
							Request("News", "#content", "show", "");
						});
					</script>
				</section>
			</main>
			<aside>
				<section id="userinfo">
					<script type="text/javascript">
						$(document).ready(function()
						{
							GetCurrentUser();
						});
					</script>
				</section>
				<section id="shoutcast">
				</section>
			</aside>
			<footer>
				<p class="copyright">Powered by [#APPNAME#] &copy; [#YEAR#] [#SITE_TITLE#]</p>
				<p class="copyright">This product includes GeoLite2 data created by <a href="http://www.maxmind.com">MaxMind</a></p>
				[#DEBUG_WARN#]
			</footer>
		</div>
	</body>
</html>
