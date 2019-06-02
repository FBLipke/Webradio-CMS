			[[site-header]]
			<nav id="navigation">
				<script type="text/javascript">
					$(document).ready(function() {
						Request("Navigation", "#navigation", "show", "");
					});
				</script>
			</nav>
		
			<main id="content">
					<script type="text/javascript">
						$(document).ready(function() {
							Request("News", "#content", "show", "");
						});
					</script>
			</main>
		
			<aside>
				<div id="right">
					<script type="text/javascript">
						$(document).ready(function() {
							Request("Users", "#right", "show", "");
						});
					</script>
				</div>
			</aside>

			<footer>
				<p class="copyright">Powered by [#APPNAME#] &copy; [#YEAR#] The Fire and Ice Radio</p>
				<p class="copyright">This product includes GeoLite2 data created by <a href="http://www.maxmind.com">MaxMind</a></p>
				[#DEBUG_WARN#]
			</footer>