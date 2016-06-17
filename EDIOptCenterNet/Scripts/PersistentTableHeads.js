// Need: to fix Opera bug whereby the scroll bar goes crazy when the bottom of the scrollable content is reached

// create persistent headers clones (upon page load)
$(".TblScroll").each(function() {
	CloneTblHeads($(this));
});
// create persistent headers clones
function CloneTblHeads(ParentDiv) {
	var clonedHeaderRow;
	ParentDiv.find(".persist-area").each(function() {
		// force scrollbar so widths will be correct even if no scroll bar upon load
		//$(this).closest('.TblScroll').css("overflow", "scroll"); // Fix scroll bug for non-window tables
		// clone row
		clonedHeaderRow = $(this).find(".persist-header");
		clonedHeaderRow.before(clonedHeaderRow.clone()).css("width", clonedHeaderRow.width()).addClass("FloatingTblHeader");
		// set th widths
		SetCloneTblHeadsWidth($(this));
		// restore auto scrollbar
		//$(this).closest('.TblScroll').css("overflow", "auto"); // Fix window scroll bug for non-window tables
	});
	// sense scrolling + trigger function
	$(window).scroll(UpdateTableHeaders).trigger("scroll");
	$('.TblScroll').scroll(UpdateTableHeaders).trigger("scroll");
}

// set cloned head width
function SetCloneTblHeadsWidth(PersistArea) {
	// resize th
	PersistArea.find('th').each(function(index) {
		var ThisWidth = $(this).width();
		PersistArea.find('.FloatingTblHeader th:eq(' + index + ')').css("width", ThisWidth);
	});
	// resize tr
	PersistArea.find('.FloatingTblHeader').css("width", PersistArea.css('width'));
}

// persistent headers positioning
function UpdateTableHeaders() {
	$(".persist-area").each(function() {
		// get elements, positions, dimensions
		var TblDiv = $(this).closest('.TblScroll'),
				WinScrollTop = $(window).scrollTop(),
				DivScrollTop = TblDiv.scrollTop(),
				FloatingTblHeader = $(".FloatingTblHeader", this),
				FloatingTblHeaderHeight = FloatingTblHeader.height();
		// set top of table div
		if ($(this).parents('.window').length) {  // in window
			var InWindow = true;
			var TblDivTopPos = TblDiv.offset().top;
		}
		else {  // not in window
			var InWindow = false;
			var TblDivTopPos = TblDiv.offset().top - 36;  // -36 for fixed header
		}
		// set floating table header position
		if ((DivScrollTop > 0) && (WinScrollTop < TblDivTopPos)) {  // scroll down inside parent div
			FloatingTblHeader.css({"visibility": "visible", "position": "absolute", "top": DivScrollTop});
		}
		else if ((WinScrollTop > TblDivTopPos) && (WinScrollTop < TblDivTopPos + TblDiv.height() - FloatingTblHeaderHeight)) {  // scroll down browser window
			if (InWindow) {
				FloatingTblHeader.css({"visibility": "visible", "position": "fixed", "top": "0"});  // top = 0 because its over fixed header
			}
			else {
				FloatingTblHeader.css({"visibility": "visible", "position": "fixed", "top": "36px"});  // top = 36px to show under fixed header
			}
		}
		else {  // hide persistent header
			FloatingTblHeader.css({"visibility": "hidden"});
		}
		;
	});
}