var WindowLgVisible = false;
var WindowMedVisible = false;
var WindowSmVisible = false;
var WindowWorkingVisible = false;

// DisplayWindow can either create a completely new window on the fly or use an
// existing container to provide a space on the page which has a window-like API.
// See the options object for a list of options. PreventCloseCallbacks and BeforeCloseCallbacks
// are great for things like confirming before closing a window with unsaved changes
// or removing a row highlight once the window has closed, respectively.
function DisplayWindow(UserOptions) {
    var self = this;

    // Gather options
    self.Options = {
        Size: null, // Mandatory for "real" windows, one of: 'large', 'medium', 'small'
        FakeContainer: null, // Mandatory for "fake" windows, creates a "window" inside the given container rather than an actual floating window
        ContainerId: null, // Optional, applies string as id attribute of container
        WindowRoot: $('#mainFrame'), // Optional, element in which to render window
        IncrementZIndex: true // Optional, places window above all other windows of the same size
    };
    $.extend(self.Options, UserOptions); // Overwrite defaults with provided options

    // Set data derived from options
    self.IsFakeWindow = (self.Options.FakeContainer !== null);
    self.SizeAbbreviation = {
        small: 'Sm',
        medium: 'Med',
        large: 'Lg'
    }[self.Options.Size];

    // Functionality and callbacks for closing the window
    self.PreventCloseCallbacks = []; // List of functions to be called before closure, a truthy return value from any will abort closure.
    self.BeforeCloseCallbacks = []; // List of functions to be called just before closure.
    self.close = function (ForceClose, AfterCloseCallback) {
        // Decide whether to allow closure
        if (ForceClose !== true) {
            var AbortClose = false;
            for (var i = 0; i < self.PreventCloseCallbacks.length; i++) {
                AbortClose |= self.PreventCloseCallbacks[i](self);
            }
            if (AbortClose) {
                return;
            }
        }

        // Notify any callbacks that we're about to close
        for (var j = 0; j < self.BeforeCloseCallbacks.length; j++) {
            self.BeforeCloseCallbacks[j](self);
        }

        // Close and destroy the window
        self.Container.fadeOut(function () {
            self.Mask.hide().remove();
            self.Container.data('DisplayWindow', null);
            if (self.IsFakeWindow) {
                self.Container.empty(); // Keep fake window container
            }
            else {
                self.Container.remove(); // Remove entire window
            }
            if (AfterCloseCallback) {
                AfterCloseCallback();
            }
        });
    };

    self.show = function () {
        if (self.IsFakeWindow) {
            self.Container.fadeIn(100);
            return;
        }

        var StartHeight = $(document).height();
        var WindowHeight = $(window).height();

        self.recenter();
        self.Mask.show();
        self.Container.fadeIn();

        // test if document size increased due to window size or window is larger than body
        var NewHeight = $(document).height();
        if (NewHeight > StartHeight || WindowHeight > NewHeight) {
            self.Mask.css('height', NewHeight + 'px');
        }
        else {
            self.Mask.css('height', '100%');
        }
    };

    self.recenter = function () {
        if (self.IsFakeWindow) {
            return;
        }
        ReposWindow(self.Container);
    };

    self.ready = function () {
        self.show();
        self.Content.find('.TblScroll').scrollTop(0);  // reset scroll bar to top
    };

    // Convenience function for loading a typical ajax page into the window
    self.LoadAjax = function (Params, DoneCallback, FailCallback) {
        BeginWorking();
        $.ajax(Params).done(function (data, textStatus, jqXHR) {
            // on success
            self.Content.html(data);
            if (DoneCallback) {
                DoneCallback(data, textStatus, jqXHR);
            }
            self.show();
            EndWorking();
        }).fail(function (jqXHR, textStatus, errorThrown) {
            // on failure
            if (FailCallback == 'alert') {
                alert(AjaxFailText);
            }
            else if (FailCallback) {
                FailCallback(jqXHR, textStatus, errorThrown);
            }
            else {
                self.Content.html(AjaxFailText);
                self.show();
            }
            EndWorking();
            self.show();
        });
    };

    self.init = function () {
        self.Content = $('<div>');
        self.Results = $('<div>').hide();

        if (self.IsFakeWindow) {
            self.Mask = $('<div>').hide(); // Dummy element
            self.Container = self.Options.FakeContainer;
            self.Container.hide();
        }
        else { // Real window
            self.Mask = $('<div>').addClass('mask').addClass('Mask' + self.SizeAbbreviation);
            self.Container = $('<div>').addClass('window').addClass('Window' + self.SizeAbbreviation);
            self.Container.html(
                '<div class="window-head"><img src="images/x.png" alt="close"  width="12" height="10" class="display-window-x"></div>' +
                '<img src="images/draggable-footer.png" width="1" height="4" class="window-head-foot" alt="rule">'
            );

            if (self.Options.IncrementZIndex) {
                // Make this window appear above any others of the same size
                var MaxIndex = $('.Window' + self.SizeAbbreviation).map(function () { return parseInt($(this).css('z-index')); }).get();
                if (MaxIndex.length > 0) {
                    MaxIndex = Math.max.apply(Math, MaxIndex);
                    self.Mask.css('z-index', MaxIndex + 1);
                    self.Container.css('z-index', MaxIndex + 2);
                }
            }

        }

        self.Container.data('DisplayWindow', self);
        if (self.Options.ContainerId) {
            // Set an ID if requested
            self.Container.attr('id', self.Options.ContainerId);
        }

        self.Container.append(self.Content, self.Results);
        self.Container.on('click', '.display-window-x, .ask-window-x, .window-x, .display-CloseWindowBtn, .ask-CloseWindowBtn, .CloseWindowBtn', function () {
            self.close();
        });

        if (!self.IsFakeWindow) {
            $(document.body).append(self.Mask);
            self.Options.WindowRoot.append(self.Container);
            self.Container.draggable({
                handle: ".window-head",
                containment: "document"
            });
        }
    };

    self.init();
    return self;
}

// reposition window
function ReposWindow(w) {
    // set top position
    var top = (($(window).height() - $(w).outerHeight()) / 2) + $(window).scrollTop();
    if (top > 0) {
        $(w).css("top", top + "px");
    }
    else {
        $(w).css("top", "20px");
    }
    // set left position
    $(w).css("left", (($(window).width() - $(w).outerWidth()) / 2) + $(window).scrollLeft() + "px");
}

// show window
function ShowWindow(w) {
    var StartHeight = $(document).height();
    var WindowHeight = $(window).height();
    // set top position
    var top = ((WindowHeight - $(w).outerHeight()) / 2) + $(window).scrollTop();
    if (top > 0) {
        $(w).css("top", top + "px");
    }
    else {
        $(w).css("top", "20px");
    }
    // set left position
    $(w).css("left", (($(window).width() - $(w).outerWidth()) / 2) + $(window).scrollLeft() + "px");
    // show mask
    if (w.hasClass('WindowLg')) {
        $('#MaskLg').show();
        WindowLgVisible = true;
    }
    if (w.hasClass('WindowMed')) {
        $('#MaskMed').show();
        WindowMedVisible = true;
    }
    if (w.hasClass('WindowSm')) {
        $('#MaskSm').show();
        WindowSmVisible = true;
    }
    if (w.is('#WindowWorking')) {
        $('#MaskWorking').show();
        WindowWorkingVisible = true;
    }
    // fade in window
    w.fadeIn();
    // test if document size increased due to window size or window is larger than body
    var NewHeight = $(document).height();
    if (NewHeight > StartHeight || WindowHeight > NewHeight) {
        $('.mask').css('height', NewHeight + 'px');
    }
    else {
        $('.mask').css('height', '100%');
    }
}

function CloseWindow(size) {
    switch (size) {
        case 'lg':
            $('.WindowLg:visible').fadeOut();
            $('#MaskLg').hide();
            WindowLgVisible = false;
            break;
        case 'med':
            $('.WindowMed:visible').fadeOut();
            $('#MaskMed').hide();
            WindowMedVisible = false;
            break;
        case 'sm':
            $('.WindowSm:visible').fadeOut();
            $('#MaskSm').hide();
            WindowSmVisible = false;
            break;
    }
}

function ShowSmallAlertWindow(title, type, msg) {
    Working.Hide();
    var statusLabel = "";
    switch (type) {
        case "good":
            statusLabel = "Success";
            break;
        case "caution":
            statusLabel = "Warning";
            break;
        case "warning":
            statusLabel = "Error";
            break;
    }
    var dWindow = new DisplayWindow({ Size: 'small' });
    dWindow.Content.append(
        $("<h2>").addClass("RedBottomHead").html(title),
        $("<p>").addClass("SmBtmMargin " + type).html(statusLabel),
        $("<div>").css("margin-bottom", "10px").html(msg),
        $("<input type='button' class='button CloseWindowBtn' value='Close Window'>")
    );
    dWindow.show();
}

var Working = (function () {
    var WorkingCount = 0;
    var WorkingTimer1;
    var WorkingTimer2;
    var WorkingTimer3;

    return {
        Show: function () {
            // If window is already open, just increment our counter
            if (WorkingCount > 0) {
                WorkingCount++;
                return;
            }

            // WorkingCount is 0, increment to 1 and show window
            WorkingCount = 1;
            // reset text

            var WorkingTxt = $('#WorkingTxt').text('Working...');
            ShowWindow($('#WindowWorking'));
            WorkingCount = 1; // Override counter in case this is called directly
            // change message after 10 seconds
            WorkingTimer1 = setTimeout(function () {
                WorkingTxt.text('We are working on your request.');
            }, 10000);
            // change message after 30 seconds
            WorkingTimer2 = setTimeout(function () {
                WorkingTxt.text('We are still working on your request. Thank you for your patience.');
            }, 30000);
            // change message after 60 seconds
            WorkingTimer3 = setTimeout(function () {
                WorkingTxt.text('While we continue to work on your request, rest assured no errors have been found.');
            }, 60000);
        },
        Hide: function () {
            // If anything is still working, leave the window open
            WorkingCount--;
            if (WorkingCount > 0) {
                return;
            }

            // Otherwise WorkingCount is 0 and nothing is working, so hide the window
            WorkingCount = 0;
            // stop timers
            clearTimeout(WorkingTimer1);
            clearTimeout(WorkingTimer2);
            clearTimeout(WorkingTimer3);
            // show main div
            $('#WindowWorking').fadeOut(400);  // 400 = default
            $('#MaskWorking').hide();
            WindowWorkingVisible = false;
        },
        Begin: function () {
            this.Show();
        },
        End: function () {
            this.Hide();
        }
    }
} ());