// Note: this file is used for the blog as well as the OC

// variables
var PartnersBtn = $('#FixedPartnersBtn');
var PartnersWindow = $('#FixedPartnersWindow');
var NotificationBtn = $('#FixedNotificationCount');
var NotificationWindow = $('#FixedNotificationsWindow');
var AccountBtn = $('#FixedAccountBtn');
var AccountWindow = $('#FixedAccountWindow');
var AjaxFailText = 'An AJAX error occured. Please contact EDI Options.';
var OnMsgListen = [];

var NotificationsDefaultDelay = 45000;  // interval for notification refreshes
// test if logged in
var WebStorage = (typeof (Storage) !== "undefined") ? true : false;  // browser supports HTML5 Web Storage?

// ------------------------------
// Begin: fixed header window functions
// ------------------------------

if (LoggedIn) {
    if ($("#FixedHeadFrame").length != 0) {
        // calculate partner window location
        var ArrowCenterPos = (PartnersBtn.offset().left + PartnersBtn.outerWidth()) - 4;
        var WinHalfWidth = PartnersWindow.outerWidth() / 2;
        var PartnersLeft = ArrowCenterPos - WinHalfWidth;
        var NotificationsLeft = ArrowCenterPos - WinHalfWidth;
        PartnersWindow.css("left", PartnersLeft + "px");
        // calculate notification window location
        if (NotificationBtn.length !== 0) {  // needed for non-RC customers
            ArrowCenterPos = (NotificationBtn.offset().left + NotificationBtn.outerWidth()) - 4;
            WinHalfWidth = NotificationWindow.outerWidth() / 2;
            NotificationWindow.css("left", NotificationsLeft + "px");
        }
        // calculate account window location
        ArrowCenterPos = (AccountBtn.offset().left + AccountBtn.outerWidth()) - 4;
        WinHalfWidth = AccountWindow.outerWidth() / 2;
        var AccountLeft = ArrowCenterPos - WinHalfWidth;
        AccountWindow.css("left", AccountLeft + "px");

        // show fixed partner window
        PartnersBtn.click(function () {
            PartnersWindow.slideToggle("fast");
            NotificationWindow.slideUp("fast");
            AccountWindow.slideUp("fast");
        });
        // show fixed notification window
        NotificationBtn.click(function () {
            if (NotificationBtn.html() !== "0") {
                PartnersWindow.slideUp("fast");
                NotificationWindow.slideToggle("fast");
                AccountWindow.slideUp("fast");
            }
        });
        // show fixed account window
        AccountBtn.click(function () {
            PartnersWindow.slideUp("fast");
            NotificationWindow.slideUp("fast");
            AccountWindow.slideToggle("fast");
        });
    }
    // close fixed windows, if the click was not within window
    $(document.body).click(function () {
        if (!PartnersWindow.has(this).length) {
            PartnersWindow.slideUp();
        }
        if (!NotificationWindow.has(this).length) {
            NotificationWindow.slideUp();
        }
        if (!AccountWindow.has(this).length) {
            AccountWindow.slideUp();
        }
    });
    // don't close fixed windows, if the click was on window
    $("#FixedPartnersBtn, #FixedPartnersWindow, #FixedNotificationCount, #FixedNotificationsWindow, #FixedAccountBtn, #FixedAccountWindow").click(function (event) {
        event.stopPropagation();
    });
}

// ------------------------------
// Begin: return home text function
// ------------------------------

// show return home div when user hovers over logo
$('#OptCenterLogo').hover(
        function () {
            $('#ReturnHome').fadeTo("fast", 1);
        },
        function () {
            $('#ReturnHome').fadeTo("fast", 0);
        }
);

$(document).on('click', '.linkHomepage', function () {
    var link = $(this).attr("data-url");
    var d = {
        Action: "Navigate",
        Url: link
    };
    //top.postMessage(JSON.stringify(d), "*");
    PostToOC("Navigate", JSON.stringify(d), function () { });
    console.log("Sent home.");
});

var eventMethod = window.addEventListener ? "addEventListener" : "attachEvent";
var eventer = window[eventMethod];
var messageEvent = (eventMethod === "attachEvent" ? "onmessage" : "message");
eventer(messageEvent, function (e) { PostToOCResponse(e); }, false);

/*
Begin: Communication with OptCenter
*/
function PostToOCResponse(e) {
    if (e.origin === "https://edi-optcenter.com" || e.origin === "https://www.edi-optcenter.com") {
        var d = JSON.parse(e.data);
        console.log(d);
        for (var i = 0; i < OnMsgListen.length; i++) {
            var listener = OnMsgListen[i];
            if (d.Action == listener.Action) {
                listener.Func(d.Data); //CALL RESPONSE FUNCTION
                OnMsgListen.splice(i, 1); //REMOVE HANDLER FOR ACTION
            }
        }
    }
};

function PostToOC(action, json, respFunc) {
    var d = {
        Action: action,
        Data: json
    };
    OnMsgListen.push({
        Action: action,
        Func: respFunc
    });
    top.postMessage(JSON.stringify(d), "*");
};
/*
End: Communication with OptCenter
*/

// ------------------------------
// Begin: set partner functions
// ------------------------------

// Set Partner
$('#FixedPartnersList li').not('.ActivePartner').click(function() {
    var PrtId = $(this).attr('data-id');
    SetActivePrt(PrtId);
});

function SetActivePrt(PrtId, page) {
    // AJAX
    var request = {
        version: 1,
        data: JSON.stringify({
            PartnerIndex: PrtId
        })
    };
    $.ajax({
        type: "POST",
        url: "EDI.asmx/ChangePartner",
        cache: false,
        data: JSON.stringify(request),
        datatype: "json",
        contentType: "application/json; charset=utf-8"
    }).done(function(html) {
        sessionStorage.NotificationsLoaded = false;
        // refresh page
        if (typeof page === 'undefined') {
            location.reload();
        }
        else {
            window.location = page;
        }
    }).fail(function() {
        alert(AjaxFailText);
        Working.End();
    });
}

// ------------------------------
// Begin: notification functions
// ------------------------------



// Load notifications
function NotificationsLoad() {
    $.ajax({
        type: "POST",
        url: RootDir + "notifications-ajax-load.php",
        cache: false
    }).done(function(html) {
        $("#FixedNotificationsWindow tbody").html(html);
        var count = $("#FixedNotificationsWindow .ReportCounterTotal td").html();
        $("#FixedNotificationsWindow .ReportCounterTotal").remove();
        // update notifications count
        NotificationsCount(count);
        // update web storage
        if (WebStorage) {
            // store notification loaded boolean
            if (sessionStorage.NotificationsLoaded !== 'true') {
                sessionStorage.NotificationsLoaded = 'true';
            }
            // store notification count
            sessionStorage.NotificationCount = count;
            // store notifications timestamp
            sessionStorage.NotificationTimestamp = new Date();
            // store notifications HTML
            sessionStorage.NotificationHTML = html;
        }
    });
}

// Refresh notifications
var NotificationsInterval;
function NotificationsRefresh(delay) {
    NotificationsInterval = setInterval(function() {
        NotificationsLoad();
    }, delay);
}

// Stop notifications refresh
function NotificationsStop() {
    clearInterval(NotificationsInterval);
}

// Notifications count display
function NotificationsCount(count) {
    NotificationBtn.html(count);
    if (count === "0") {
        NotificationBtn.removeClass("notifications-found");
        NotificationBtn.attr("title", "You have 0 new reports.");
    }
    else {
        NotificationBtn.addClass("notifications-found");
        var NotificationTitle = (count === "1") ? "You have " + count + " new report!" : "You have " + count + " new reports!";
        NotificationBtn.attr("title", NotificationTitle);
    }
}

// Notification Header Clicked: go to reports center
NotificationWindow.on("click", "th", function() {
    document.location = RootDir + "reports.php";
});
// Notification Row Clicked: go to reports center with selected trx
NotificationWindow.on("click", "tbody tr", function() {
    var trx = $(this).attr("id").substr(0, 3);
    document.location = RootDir + "reports.php?trx=" + trx;
});

// Update report(s) status
function UpdateReports(action, val, flag) {
    $.ajax({
        type: "POST",
        data: {action: action, ids: val, f: flag},
        url: RootDir + "reports-ajax-flag.php",
        cache: false
    }).done(function() {
        NotificationsLoad();
    });
}

// ------------------------------
// Begin: window functions
// ------------------------------

// make draggable
$(".window").draggable({
    handle: ".window-head",
    containment: "document"
});

// Small window (not in use yet, but want to move to a protocol like this for window loading)
if (($('#EmptyWindowSm').length > 0)) {
    var EmptyWindowSm = $('#EmptyWindowSm');
    var EmptyDivSm = $('#EmptyDivSm');
    var EmptyDivSmResult = $('#EmptyDivSmResult');
    function WindowSmReady2(ShowDiv) {
        // show hide divs
        if (ShowDiv === EmptyDivSm) {  // no need for WindowSmLoad function because of this
            EmptyDivSm.show();
            EmptyDivSmResult.hide();
        }
        else {
            EmptyDivSm.hide();
            EmptyDivSmResult.show();
        }
        // show or reposition window
        if (WindowSmVisible === true) {
            ReposWindow(EmptyWindowSm);
        }
        else {
            ShowWindow(EmptyWindowSm);
        }
        // final preperations
        WindowReady(EmptyWindowSm);
    }
}

// Medium window (not in use yet, but want to move to a protocol like this for window loading)
if (($('#EmptyWindowMed').length > 0)) {
    var EmptyWindowMed = $('#EmptyWindowMed');
    var EmptyDivMed = $('#EmptyDivMed');
    var EmptyDivMedResult = $('#EmptyDivMedResult');
    function WindowMedReady2(ShowDiv) {
        // show hide divs
        if (ShowDiv === EmptyDivMed) {  // no need for WindowMedLoad function because of this
            EmptyDivMed.show();
            EmptyDivMedResult.hide();
        }
        else {
            EmptyDivMed.hide();
            EmptyDivMedResult.show();
        }
        // show or reposition window
        if (WindowMedVisible === true) {
            ReposWindow(EmptyWindowMed);
        }
        else {
            ShowWindow(EmptyWindowMed);
        }
        // final preperations
        WindowReady(EmptyWindowMed);
    }
}

// Large window (not in use yet, but want to move to a protocol like this for window loading)
if (($('#EmptyWindowLg').length > 0)) {
    var EmptyWindowLg = $('#EmptyWindowLg');
    var EmptyDivLg = $('#EmptyDivLg');
    var EmptyDivLgResult = $('#EmptyDivLgResult');
    function WindowLgReady2(ShowDiv) {
        // show hide divs
        if (ShowDiv === EmptyDivLg) {  // no need for WindowLgLoad function because of this
            EmptyDivLg.show();
            EmptyDivLgResult.hide();
        }
        else {
            EmptyDivLg.hide();
            EmptyDivLgResult.show();
        }
        // show or reposition window
        if (WindowLgVisible === true) {
            ReposWindow(EmptyWindowLg);
        }
        else {
            ShowWindow(EmptyWindowLg);
        }
        // final preperations
        WindowReady(EmptyWindowLg);
    }
}

// post window load preperation
function WindowReady(w) {
    // reset scroll bar to top
    w.find('.TblScroll').scrollTop(0);
    // create tbl head clones
    CloneTblHeads(w);  // shoud move PersistentTableHeads.js file content into this file for speed
    // initialize table arrays
    w.find('.toolbar').each(function() {
        var TblId = $(this).attr('data-rel-tbl');
        StartTblArr(TblId);
    });
// hide working window
    Working.End();
}

// close window
function CloseWindowInternal(w) {
    if (w.hasClass('WindowLg')) {
        CloseWindow('lg');
    }
    if (w.hasClass('WindowMed')) {
        CloseWindow('med');
    }
    if (w.hasClass('WindowSm')) {
        CloseWindow('sm');
    }
}
$('.window').on('click', '.window-x, .CloseWindowBtn', function() {
    CloseWindowInternal($(this).closest('.window'));
});


// should try to make this (below) work so this functionality does not need to be coded for every page
//
// store highlighted TblRows
//var HighlightFrameArr = new Array();
//var HighlightTrArr = new Array();
//function HighlightTr(Frame, TblRow) {
//	HighlightFrameArr.push(Frame);
//	HighlightTrArr.push(TblRow);
//	TblRow.addClass('TblRowHighlight');
//}
//
// remove tr highlighting
//function UnhighlightTr() {
//	var TblRow = HighlightTrArr[-1];
//	TblRow.removeClass('TblRowHighlight');
//	HighlightTrArr.pop();
//	HighlightFrameArr.pop();
//}

// capture all focus events to make sure focus does not escape windows
$('body').on('focus', ':input, a', function () {
    // exclude jQuery datepicker interactions
    var FocusFrame = null;
    if ($(this).parents('#ui-datepicker-div').length === 0) {
        // determine where focus was obtained
        if ($(this).parents('.window').length > 0) {
            if ($(this).closest('.window').hasClass('WindowLg')) {
                FocusFrame = 'WindowLg';
            }
            else if ($(this).closest('.window').hasClass('WindowMed')) {
                FocusFrame = 'WindowMed';
            }
            else {
                FocusFrame = 'WindowSm';
            }
        }
        else {
            FocusFrame = 'body';
        }
    }

    // regarding where focus was obtained
    switch (FocusFrame) {
        case 'body':
            if (WindowLgVisible || WindowMedVisible || WindowSmVisible || WindowWorkingVisible) {
                RedirectFocus();
            }
            break;
        case 'WindowLg':
            if (WindowMedVisible || WindowSmVisible || WindowWorkingVisible) {
                RedirectFocus();
            }
            break;
        case 'WindowMed':
            if (WindowSmVisible || WindowWorkingVisible) {
                RedirectFocus();
            }
            break;
        case 'WindowSm':
            if (WindowWorkingVisible) {
                RedirectFocus();
            }
            break;
    }
});
// redirect/remove focus
function RedirectFocus() {
    // find highest level visible window
    var TopWin = null;
    if (WindowLgVisible && !WindowMedVisible && !WindowSmVisible && !WindowWorkingVisible) {
        TopWin = $('.WindowLg:visible');
    }
    else if (WindowMedVisible && !WindowSmVisible && !WindowWorkingVisible) {
        TopWin = $('.WindowMed:visible');
    }
    else if (WindowSmVisible && !WindowWorkingVisible) {
        TopWin = $('.WindowSm:visible');
    }
    else {
        TopWin = $('#WindowWorking');
    }
    // find window's first focusable element
    var FirstInput = TopWin.find(':input:visible:not(input[type="button"]):first');
    if (FirstInput.length === 0) {
        FirstInput = TopWin.find(':input:visible:first');
    }
    if (FirstInput.length === 0) {
        FirstInput = TopWin.find('a:visible:first');
    }
    // if no focusable element blur focus
    if (FirstInput.length === 0) {
        $(':focus').blur();
    }
    // redirect focus
    else {
        FirstInput.focus();
    }
}

// ------------------------------
// Begin: contact support functions
// ------------------------------

// pull out tab on hover
$('#supportBtn').hover(
        function() { // on mouse over
            $(this).attr('src', '/images/side-help-on.png');
        },
        function() {  // on mouse exit
            $(this).attr('src', '/images/side-help.png');
        }
);

// open window
$('#supportBtn').click(function() {
    $('#SupportFormDiv').show();
    $('#SupportResult').hide();
    ShowWindow($('#supportWindow'));
});

// ------------------------------
// Begin: table striping function
// ------------------------------

// strip tables upon page load
$('.StripedTbl').each(function() {
    StripeRows($(this));
});

// stripe table with blue rows
function StripeRows(tbl) {
    var i = 0;
    tbl.find('.blueBg').each(function() {
        var tr = $(this);
        // find only visible (can't use :visible because of IE bug)
        if (tr.css('display') !== 'none') {
            tr.removeClass('TblRowLight TblRowDark');
            if (i % 2 === 0) {  // even
                tr.addClass('TblRowLight');  // even, first element
            }
            else {  // odd
                tr.addClass('TblRowDark');
            }
            i++;
        }
    });
}

// ------------------------------
// Begin: table sorting functions
// ------------------------------

/*
 * Natural Sort algorithm for Javascript - Version 0.7 - Released under MIT license
 * Author: Jim Palmer (based on chunking idea from Dave Koelle)
 * https://github.com/overset/javascript-natural-sort
 */
 function naturalSort (a, b) {
    var re = /(^-?[0-9]+(\.?[0-9]*)[df]?e?[0-9]?$|^0x[0-9a-f]+$|[0-9]+)/gi,
        sre = /(^[ ]*|[ ]*$)/g,
        dre = /(^([\w ]+,?[\w ]+)?[\w ]+,?[\w ]+\d+:\d+(:\d+)?[\w ]?|^\d{1,4}[\/\-]\d{1,4}[\/\-]\d{1,4}|^\w+, \w+ \d+, \d{4})/,
        hre = /^0x[0-9a-f]+$/i,
        ore = /^0/,
        i = function(s) { return naturalSort.insensitive && (''+s).toLowerCase() || ''+s },
        // convert all to strings strip whitespace
        x = i(a).replace(sre, '') || '',
        y = i(b).replace(sre, '') || '',
        // chunk/tokenize
        xN = x.replace(re, '\0$1\0').replace(/\0$/,'').replace(/^\0/,'').split('\0'),
        yN = y.replace(re, '\0$1\0').replace(/\0$/,'').replace(/^\0/,'').split('\0'),
        // numeric, hex or date detection
        xD = parseInt(x.match(hre)) || (xN.length != 1 && x.match(dre) && Date.parse(x)),
        yD = parseInt(y.match(hre)) || xD && y.match(dre) && Date.parse(y) || null,
        oFxNcL, oFyNcL;
    // first try and sort Hex codes or Dates
    if (yD)
        if ( xD < yD ) return -1;
        else if ( xD > yD ) return 1;
    // natural sorting through split numeric strings and default strings
    for(var cLoc=0, numS=Math.max(xN.length, yN.length); cLoc < numS; cLoc++) {
        // find floats not starting with '0', string or 0 if not defined (Clint Priest)
        oFxNcL = !(xN[cLoc] || '').match(ore) && parseFloat(xN[cLoc]) || xN[cLoc] || 0;
        oFyNcL = !(yN[cLoc] || '').match(ore) && parseFloat(yN[cLoc]) || yN[cLoc] || 0;
        // handle numeric vs string comparison - number < string - (Kyle Adams)
        if (isNaN(oFxNcL) !== isNaN(oFyNcL)) { return (isNaN(oFxNcL)) ? 1 : -1; }
        // rely on string comparison if different types - i.e. '02' < 2 != '02' < '2'
        else if (typeof oFxNcL !== typeof oFyNcL) {
            oFxNcL += '';
            oFyNcL += '';
        }
        if (oFxNcL < oFyNcL) return -1;
        if (oFxNcL > oFyNcL) return 1;
    }
    return 0;
}

// Uses the given column to sort the given table in the given order.
// Separated from the event handler so it can be re-used in table.js.
function TableSort(TdIndex, TBody, Asc, SortKeyMapper) {
    // TdIndex: Column index to sort by
    // TBody: <tbody> containing rows to sort
    // Asc: Bool whether to sort asc, otherwise desc
    // SortKeyMapper: Function, a cell is passed in, it must return the text to sort by
    // Using the built-in sort is preferable to our own sort, but that would normally
    // require transforming cells to text many times, which would impact performance.
    // Here, we compute the text contents once per td to sort, then sort based on that.
    // https://developer.mozilla.org/en-US/docs/Web/JavaScript/Reference/Global_Objects/Array/sort#Sorting_maps

    // Transform from 0-indexed to 1-indexed, this uses nth-child() rather than eq().
    // This is faster and allows us to select all cells at once.
    TdIndex++;

    // Clone each row and associate it with the text of the td to sort by
    var ClonedValues = TBody.find('tr td:nth-child(' + TdIndex + ')').map(function(i, Cell) {
        var $Cell = $(Cell);
        return {
            SortKey:   SortKeyMapper($Cell),
            ClonedRow: $Cell.parent().clone(true)[0]
        };
    }).get();

    // Sort by precomputed SortKey
    // JS does not provide a stable sort; if the SortKey is the same for two rows, results may be inconsistent.
    ClonedValues.sort(function(a, b) {
        return naturalSort(a.SortKey, b.SortKey);
    });

    // Array.sort is normally asc, reverse if desc is needed
    if (!Asc) {
        ClonedValues.reverse();
    }

    // Grab only the cloned table rows, discarding SortKey
    var SortedRows = $.map(ClonedValues, function(Value) { return Value.ClonedRow; });

    // Replace the old rows with the rows we have sorted
    TBody.empty().append.apply(TBody, SortedRows);
}

//Sort table by header clicked
$(document).on("click", ".Sortable", function() {
    var index = $(this).index(); //column index
    var tbl = $(this).closest("table"); //get table
    var tbody = tbl.find("tbody"); //get body of table
    var asc = !($(this).hasClass("SortAsc")); //set direction to sort

    tbl.find("th").removeClass("SortAsc SortDesc"); //clear arrow on column headers
    $(tbl).find("thead tr").find("th:eq(" + index + ")").addClass((asc ? "SortAsc" : "SortDesc")); //show arrow on persist-headers

    // TableSort should be usable here and for the table display protocol.
    // The old behavior (this) is to sort by the html() of the td. This is fine for
    // cells which never change, but if an <input> is changed, for example, the
    // .html() will still yield the original value, which leads to the wrong sort
    // order. I'm leaving this old behavior here for fear of breaking something,
    // but everything using the table display protocol will sort by the current
    // value instead.
    var SortKeyMapper = function($Cell) {
        return $Cell.html().toLowerCase();
    };

    TableSort(index, tbody, asc, SortKeyMapper); // Perform the actual sorting
    StripeRows(tbl); //stripe table
});

// ------------------------------
// Begin: table collections functions
// ------------------------------

// collection check/uncheck all
$(document).on('click', '.CollAllCheckbox input', function () {
    var TblId = $(this).closest('table').attr('data-rel-tbl');
    if (!TblId) {
        TblId = $(this).closest('table').attr('id');
    }
    // checked
    if ($(this).prop('checked')) {
        $('#' + TblId + ' .CollAllCheckbox input').prop('checked', true);  // copy state to persistent header & vice versa
        $('#' + TblId + ' .CollCheckbox:visible input').prop("checked", true);
        $('#' + TblId + ' .CollCheckbox:hidden input').prop("checked", false);
    }
    // unchecked
    else {
        $('#' + TblId + ' .CollAllCheckbox input').attr('checked', false);  // copy state to persistent header & vice versa
        $('#' + TblId + ' .CollCheckbox input').attr("checked", false);
    }
    CollectionChange(TblId);
});

// collection check/uncheck individual
$('body').on('change', '.CollCheckbox', function() {
    var TblId = $(this).closest('table').attr('id');
    CollectionChange(TblId);
});

var CollEmptyText = 'An item collection is needed to use this function. Use the checkboxes to make an item collection.';
var TblArr = [];
var CollCount = 0;

// initialize arrays for each table
$('.toolbar').each(function() {
    var TblId = $(this).attr('data-rel-tbl');
    StartTblArr(TblId);
});
function StartTblArr(TblId) {
    TblArr[TblId] = [];
    TblArr[TblId]['CollCount'] = 0;
    TblArr[TblId]['UniqueKeyArr'] = [];
}

// collection change
function CollectionChange(TblId) {
    // reset & initailize arrays
    TblArr[TblId] = [];
    TblArr[TblId]['UniqueKeyArr'] = [];
    // reset counter
    CollCount = 0;
    // loop through each tr checkbox
    $('#' + TblId + ' .CollCheckbox input').each(function() {
        if ($(this).prop('checked')) {
            CollCount++;
            // add unique keys to array
            TblArr[TblId]['UniqueKeyArr'].push($(this).closest('tr').attr('data-unique-key'));
        }
    });
    // update counter
    $('.toolbar[data-rel-tbl="' + TblId + '"] .CollCount').text(CollCount);
    TblArr[TblId]['CollCount'] = CollCount;
}

// tooltips
$('#mainFrame').on('click', '.tooltip-link', function () {
    // open
    if ($(this).attr('src') === 'images/question-mark.png') {
        $(this).next().fadeIn("fast", "swing");
        $(this).attr("src", "images/tooltip-close-btn.png");
    }
    // close
    else {
        $(this).attr("src", "images/question-mark.png");
        $(this).next().fadeOut("fast", "swing");
    }
});

function submitAJAXRequest(pageLink, requestDataObj, goodFunc, failFunc) {
    var payload = JSON.stringify({
        version: 1,
        data: JSON.stringify(requestDataObj)
    });
    $.ajax({
        type: "POST",
        url: pageLink,
        cache: false,
        data: payload,
        dataType: "json",
        contentType: "application/json; charset=utf-8",
        success: goodFunc,
        error: failFunc || defaultFailureFunc
    });
}

function defaultFailureFunc(jqXHR, exception) {
    Working.Hide();
    var dWindow = new DisplayWindow({ Size: 'small' });
    dWindow.Content.append(
        $("<div>").html("An AJAX error occurred. Please contact EDI Options."),
        $("<input type='button' class='button CloseWindowBtn' value='Close Window'>")
    );
    dWindow.show();
}

function ShowMessage(title, msg, isSuccess) {
    Working.Hide();
    var dWindow = new DisplayWindow({ Size: 'small' });
    dWindow.Content.append(
        $("<h2>").addClass("RedBottomHead").html(title),
        $("<p>").addClass((isSuccess ? "good" : "warning") + " SmBtmMargin").html((isSuccess ? "Success" : "Error") + "!"),
        $("<p>").addClass("SmallGreyTxt BtmMargin").html(msg.replace("\r\n", "<br><br>")),
        $("<input type='button'>").addClass("button CloseWindowBtn").prop("value", "Close Window")
    );
    dWindow.show();
}

function AddUploadControl(controlContainer, onControlLoad) {
    var goodFunc = function (response) {
        var resp = JSON.parse(response.d);
        if (resp.success) {
            var frame = $("<iframe>").attr({ "src": "upload/" + resp.data, "seamless": "seamless", "width": 400, "height": 40 });
            // Hook the page load
            
            frame.load(function () {
                onControlLoad();
            });
            $(controlContainer).append(frame);
        }
    }
    submitAJAXRequest("EDI.asmx/GetKey", "", goodFunc);
}

// -- Table Creation --
function createCollapsibleSection(headerText, key, content, onOpen) {
    var handleContainer = $("<p>").addClass("expand-handle").attr("id", key + "_handle").click(function () {
        if ($(this).hasClass('expand-handle-open')) {
            $(this).next("div").slideUp();
            $(this).removeClass("expand-handle-open");
        }
        else {
            $(this).addClass("expand-handle-open");
            $(this).next("div").slideDown();
            if (onOpen) {
                onOpen($(this).next("div"));
            }
        }
    }).append($("<span>").html(headerText));

    var dataContainer = $("<div>").addClass("expand-content").attr("data-prtid", key).css("border-bottom", "0px").append(
        content
    );
    return $("<div>").append(
        handleContainer,
        dataContainer
    );
}

function createDataTable(tableID, dataColCount, dataRowCount, thFunc, trFunc) {
    var key = genKey();
    var divContent = $("<div id='" + tableID + "'>");
    if (dataColCount <= 0) {
        return divContent;
    }

    var trHead = $("<tr>").prop("align", "center").css({ "border-color": "transparent", "font-size": "14px" });

    for (var i = 0; i < dataColCount; i++) {
        trHead.append(thFunc(i));
    }

    var thead = $("<thead>").append(trHead);
    var tbody = $("<tbody>");

    for (var i = 0; i < dataRowCount; i++) {
        tbody.append(trFunc(i));
    }

    var headTable = $("<table class='TblHeader PaddedTbl'>").append(thead);
    var reportTable = $("<table>").prop({ "class": "dataTable prevTable blueBg PaddedTbl scrollTable", "cellspacing": "0", "rules": "all", "border": "1", "id": "detailTable" }).css("border-collapse", "collapse").append(
            tbody
        );

    divContent.append(headTable);
    var divScroll = $("<div class='BtmMargin TblScroll TblScrollShort'>").append(reportTable);
    divContent.append(divScroll);
    return divContent;
}

function applyTableStyling(jqTableElement) {
    var rows = null;
    if (jqTableElement) {
        rows = $(".dataTable > tbody > tr", $(jqTableElement));
    }
    else {
        rows = $(".dataTable > tbody > tr");
    }
    rows.removeClass("TblRowLight").removeClass("TblRowDark");
    rows.filter(":odd").addClass("TblRowLight");
    rows.filter(":even").addClass("TblRowDark");
}

function applyTableHeaderStyle(div) {
    var TableHead = div.find('.TblHeader');
    var TableBody = TableHead.next().find('.PaddedTbl');
    var HeaderCols = TableHead.find('th');
    var DataCols = TableBody.find('tr:visible:first td');
    var Length = HeaderCols.length;
    for (var i = 0; i < Length; i++) {
        $(HeaderCols[i]).css('width', $(DataCols[i]).css('width'));
    }
    TableHead.find('tr').css('width', TableBody.find('tr:visible:first').css('width')).css('display', 'block');
}

function genKey(len) {
    if (!len) {
        len = 8;
    }
    var usedKey = "";
    var possible = "ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz0123456789";
    for (var i = 0; i < len; i++) {
        usedKey += possible.charAt(Math.floor(Math.random() * possible.length));
    }
    return usedKey;
}

function arrayContains(array, obj) {
    return $.inArray(obj, array) > -1;
}

function isDate(dateStr) {
    if (dateStr === null || dateStr === undefined || typeof (dateStr) !== "string") {
        return false;
    }
    return isNaN(new Date(dateStr)) === false;
}

// -- End Table Creation --

(function () {
    var appsPresent = false;
    for (var i = 0; i < Object.keys(appList).length; i++) {
        appsPresent = true;
        var key = "app" + i;
        var name = Object.keys(appList)[i];
        var url = appList[name];
        $("#menu .CriteriaDiv").append(
            $("<a>").prop("href", url).append(
                $("<div>").addClass("RepeatableDiv StripedDiv").append(
                    $("<span>").css({ "font-weight": "bold", "font-size": "1.2em" }).html(name)
                )
            )
        );
    }

    if (appsPresent) {
        $("#noAppsSpan").hide();
    }

    $("#menu .StripedDiv:visible:odd").css("background-color", "#252525");
    $("#menu .StripedDiv:visible:even").css("background-color", "");

    $(".pageContent").addClass("push");
    $('#drawerButton').bigSlide({ speed: 500 });

    (function () {
        var emailGood = true;
        var msgGood = false;
        var mathGood = false;
        var def_name = $("#cs_name").val();
        var def_company = $("#cs_co").val();
        var def_email = $("#cs_email").val();
        var def_msg = $("#cs_notes").val();

        $("#cs_email").focusout(function () {
            if ($("#cs_email_err").length === 0) {
                $("#cs_email").after(
                $("<label>").prop({ "id": "cs_email_err", "for": "cs_email" }).addClass("error").css("display", "none").html("Valid email address required.")
            );
            }
            emailGood = /^[a-zA-Z0-9.!#$%&'*+\/=?^_`{|}~-]+@[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?(?:\.[a-zA-Z0-9](?:[a-zA-Z0-9-]{0,61}[a-zA-Z0-9])?)*$/.test($(this).val());
            if (!emailGood) {
                $("#cs_email_err").show();
            }
            else {
                $("#cs_email_err").hide();
            }
        });

        $("#cs_notes").focusout(function () {
            if ($("#cs_notes_err").length === 0) {
                $("#cs_notes").after(
                $("<label>").prop({ "id": "cs_notes_err", "for": "cs_notes" }).addClass("error").css("display", "none").html("Please enter a message.")
            );
            }
            msgGood = $.trim($(this).val()).length > 0;
            if (!msgGood) {
                $("#cs_notes_err").show();
            }
            else {
                $("#cs_notes_err").hide();
            }
        });

        $("#cs_mathResponse").focusout(function () {
            if ($("#cs_mathResponse_err").length === 0) {
                $("#cs_mathResponse").after(
                $("<label>").prop({ "id": "cs_mathResponse_err", "for": "cs_mathResponse" }).addClass("error").css("display", "none")
            );
            }
            var answer = $.trim($(this).val());
            if (answer.length === 0) {
                $("#cs_mathResponse_err").html("Please add the numbers.").show();
                mathGood = false;
            }
            else if (parseInt(answer) !== cs_mathSolution) {
                $("#cs_mathResponse_err").html("Your math is incorrect.").show();
                mathGood = false;
            }
            else {
                $("#cs_mathResponse_err").hide();
                mathGood = true;
            }
        });

        $("#cs_reset").click(function () {
            $("#cs_name").val(def_name);
            $("#cs_co").val(def_company);
            $("#cs_email").val(def_email);
            $("#cs_notes").val(def_msg);
        });

        $("#cs_submit").click(function () {
            if (emailGood && msgGood && mathGood) {
                var name = $("#cs_name").val();
                var company = $("#cs_co").val();
                var email = $("#cs_email").val();
                var msg = $("#cs_notes").val();
                var doneFunc = function (resp) {
                    var response = JSON.parse(resp.d);

                    $('#SupportResult').html(response.data);
                    $('#SupportFormDiv').hide();
                    $('#SupportResult').show();

                    Working.End();
                }
                var failFunc = function (xhr, status, eTxt) {
                    
                    $('#SupportResult').html("An AJAX error occurred. Please contact EDI Options.");
                    $('#SupportFormDiv').hide();
                    $('#SupportResult').show();

                    Working.End();
                }

                var payload = {
                    version: 1,
                    data: JSON.stringify({
                        Name: name,
                        Company: company,
                        Email: email,
                        Message: msg
                    })
                };
                Working.Begin();
                var sendURL = "EDI.asmx/SubmitSupportRequest";
                $.ajax({
                    type: "POST",
                    url: sendURL,
                    cache: false,
                    data: JSON.stringify(payload),
                    dataType: "json",
                    contentType: "application/json; charset=utf-8",
                    success: doneFunc,
                    error: failFunc
                });
            }
        });
    } ());
}());

// ------------------------------
// Begin: Common repeated UI snippets
// ------------------------------
var ui = {};
ui.TableRow = function (UniqueKey) {
    return $('<tr>').addClass('blueBg').attr('data-unique-key', UniqueKey);
};
ui.CollCheckbox = function () {
    return $('<input type="checkbox" title="Add item to collection" class="CollCheckbox">');
};
ui.DateOutput = function (input) {
    if (input == '0000-00-00' || input === null) {
        return '';
    }
    if (typeof input !== 'object') {
        input = input.split(' ')[0]; // Take only date segment
        input = input.replace(/-/g, '/'); // Replace dashes with slashes, which forces JS to parse as local time
        input = new Date(input);
    }

    var sortable = $.datepicker.formatDate('yymmdd000000', input);
    var shortdesc = $.datepicker.formatDate('M dd yy', input);
    var longdesc = input.toString(); //TODO better formatting?

    var output = $('<span>').addClass('DateOutput');
    output.append($('<span>').addClass('DateSortable').hide().text(sortable));
    output.append($('<abbr>').attr('title', longdesc).text(shortdesc));
    return output;
};
ui.FileTypeIcon = function (type) {
    return $('<img>').attr('border', '0').attr('alt', type.toUpperCase()).attr('src', 'images/file-' + type.toLowerCase() + '-sm.png');
};
ui.FileTypeIconLink = function (type, href) {
    return $('<a>').attr('href', href).attr('title', type.toUpperCase()).attr('target', '_blank').append(ui.FileTypeIcon(type));
};
ui.Button = function (text) {
    return $('<button type="button">').addClass('button').html(text);
};
ui.ButtonLink = function (text) {
    return $('<a>').attr('target', '_blank').addClass('button').html(text);
};
ui.ButtonSmall = function (text) {
    return ui.Button(text).addClass('btnSm');
};
ui.ButtonClose = function (text) {
    return ui.Button(text || 'Close Window').addClass('display-CloseWindowBtn');
};
ui.SubmissionError = function (Message, Title) {
    Title = Title || 'Submission Error!'; // Default to this value
    var output = $('<div>').addClass('SubmissionError');
    output.append($('<p>').addClass('warning SmBtmMargin').text(Title));
    output.append($('<p>').addClass('SmallGreyTxt BtmMargin').html(Message));
    return output;
};
ui.QuantityStatus = function (PresentQty, TotalQty) {
    PresentQty = parseFloat(PresentQty || 0, 10);
    TotalQty = parseFloat(TotalQty || 0, 10);
    var StatusClass = (PresentQty < TotalQty) ? 'caution' : 'good';
    var StatusText = PresentQty + '/' + TotalQty;
    return $('<span>').addClass(StatusClass).text(StatusText);
};
ui.WindowHeader = function (Message) {
    return $('<h2>').addClass('RedBottomHead SmBtmMargin').html(Message);
};
ui.Clearfix = function (Element) {
    // Clearfix an element or creates a clearfix div
    Element = Element || $('<div>');
    Element.addClass('clearfix');
    return Element;
};
ui.Tooltip = function (Message, PreviousElement) {
    var Tooltip = $('<span>').addClass('tooltip-parent');
    var Content = $('<div>').html(Message);
    Tooltip.append($('<img src="images/question-mark.png" width="17" height="16" alt="?" class="tooltip-link" />'));
    Tooltip.append($('<div>').addClass('tooltip-content').append(
		$('<img src="images/tooltip-arrow.png" width="11" height="12" alt="arrow" class="tooltip-arrow" />'),
		Content
	));
    Tooltip.Content = Content; // For the caller to use
    if (PreviousElement) {
        PreviousElement.addClass('tooltip-head');
        ui.Clearfix(Tooltip);
    }
    return Tooltip;
};
ui.Abbr = function (val, max) {
    return $("<abbr>").attr("title", val).html((val.length > max ? val.substring(0, max) + "..." : val));
};
ui.Alert = function (title, msg, stat) {
    var ow = new DisplayWindow({ Size: 'small' });
    ow.Content.append(ui.WindowHeader(title));
    var x;
    switch (stat) {
        case "G": { x = $('<p>').addClass('good SmBtmMargin').text('Success!'); break; }
        case "W": { x = $('<p>').addClass('caution SmBtmMargin').text('Warning!'); break; }
        case "E": { x = $('<p>').addClass('warning SmBtmMargin').text('Error!'); break; }
    }
    ow.Content.append(x);
    ow.Content.append($('<p>').addClass('SmallGreyTxt').html(msg));
    ow.Content.append(ui.Button("OK").addClass('CloseWindowBtn'));
    ow.show();
    return ow;
};
ui.AlertGood = function (title, msg) { ui.Alert(title, msg, "G"); };
ui.AlertWarn = function (title, msg) { ui.Alert(title, msg, "W"); };
ui.AlertError = function (title, msg) { ui.Alert(title, msg, "E"); };
ui.Prompt = function (title, msg, onYes, onYesParam, yTxt, nTxt) {
    yTxt = (typeof yTxt === 'undefined' ? "SUBMIT" : yTxt);
    nTxt = (typeof nTxt === 'undefined' ? "CLOSE WINDOW" : nTxt);
    var ow = new DisplayWindow({ Size: 'small' });
    ow.Content.append(ui.WindowHeader(title));
    ow.Content.append($('<p>').addClass('SmallGreyTxt BtmMargin').html(msg));
    var btnYes = ui.Button(yTxt).addClass('CloseWindowBtn').on('click', function () { onYes(onYesParam); });
    ow.Content.append($("<div>").append(btnYes, ui.Button(nTxt).css("margin-left", "15px").addClass("CloseWindowBtn")));
    ow.show();
    return ow;
};