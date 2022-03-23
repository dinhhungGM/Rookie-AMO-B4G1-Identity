self.collectMediaQueries = function () {
	var viewIds = self.getViewIds();
	var styleSheet = self.getApplicationStylesheet();
	var cssRules = self.getStylesheetRules(styleSheet);
	var numOfCSSRules = cssRules ? cssRules.length : 0;
	var cssRule;
	var id = viewIds.length ? viewIds[0] : ""; // single view
	var selectorIDText = "#" + id;
	var selectorClassText = "." + id + "_Class";
	var viewsNotFound = viewIds.slice();
	var viewsFound = [];
	var selectorText = null;
	var property = self.prefix + "view-id";
	var stateName = self.prefix + "state";
	var stateValue;

	for (var j = 0; j < numOfCSSRules; j++) {
		cssRule = cssRules[j];

		if (cssRule.media) {
			var mediaRules = cssRule.cssRules;
			var numOfMediaRules = mediaRules ? mediaRules.length : 0;
			var mediaViewInfoFound = false;
			var mediaId = null;

			for (var k = 0; k < numOfMediaRules; k++) {
				var mediaRule = mediaRules[k];

				selectorText = mediaRule.selectorText;

				if (selectorText == ".mediaViewInfo" && mediaViewInfoFound == false) {

					mediaId = self.getStyleRuleValue(mediaRule, property);
					stateValue = self.getStyleRuleValue(mediaRule, stateName);

					selectorIDText = "#" + mediaId;
					selectorClassText = "." + mediaId + "_Class";

					// prevent duplicates from load and domcontentloaded events
					if (self.addedViews.indexOf(mediaId) == -1) {
						self.addView(mediaId, cssRule, mediaRule, stateValue);
					}

					viewsFound.push(mediaId);

					if (viewsNotFound.indexOf(mediaId) != -1) {
						viewsNotFound.splice(viewsNotFound.indexOf(mediaId));
					}

					mediaViewInfoFound = true;
				}

				if (selectorIDText == selectorText || selectorClassText == selectorText) {
					var styleObject = self.viewsDictionary[mediaId];
					if (styleObject) {
						styleObject.styleDeclaration = mediaRule;
					}
					break;
				}
			}
		}
		else {
			selectorText = cssRule.selectorText;

			if (selectorText == null) continue;

			selectorText = selectorText.replace(/[#|\s|*]?/g, "");

			if (viewIds.indexOf(selectorText) != -1) {
				self.addView(selectorText, cssRule, null, stateValue);

				if (viewsNotFound.indexOf(selectorText) != -1) {
					viewsNotFound.splice(viewsNotFound.indexOf(selectorText));
				}

				break;
			}
		}
	}

	if (viewsNotFound.length) {
		console.log("Could not find the following views:" + viewsNotFound.join(",") + "");
		console.log("Views found:" + viewsFound.join(",") + "");
	}
}

document.querySelector('#togglePassword').addEventListener('click', function (e) {
	const password = document.querySelector('#password');
	// toggle the type attribute
	const type = password.getAttribute('type') === 'password' ? 'text' : 'password';
	password.setAttribute('type', type);
	// toggle the eye / eye slash icon
	this.classList.toggle('bi-eye');
});
