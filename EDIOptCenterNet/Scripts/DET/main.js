var currentVideoPosition = 0;
var numVideos = 0;

$(document).ready(function () {   
    var demoPageType = ""
    submitAJAXRequest("DemoTemplate.aspx/getYouTubeLinks", demoPageType, function (response) {
        var resp = JSON.parse(response.d);
        debugger;
        if (resp.success) {            
            var ytvideos = resp.data.videos;
            numVideos = ytvideos.length;
            if (numVideos > 0) {                
                $(".videoplayer").append($("<h1>").prop({ "text-align" : "center","id":"titleDisplay" }).html("<b>"+ytvideos[0].title+"<b>"));
                $(".videoplayer").css({ 'margin-left': "500px"});               
                $(".MainContent").css({ "width": "2000px" });
                var div = document.getElementsByClassName("videoplayer");
                $('<iframe>', {
                    width: 900,
                    height: 500,
                    src: "",
                    id: 'videoIframe',
                    allowFulLScreen: '',
                    frameborder: 0,
                    scrolling: 'no',
                }).appendTo('.videoplayer');
                currentVideoPosition = 1;
                changeVideoDisplay(ytvideos[0]);                
            }            
            if (ytvideos.length > 1) {
                $(".videoplayer").append($("<button>").prop({ "type": "button", "name": "prevbutton" }).addClass("button").addClass("divider").css({ "width": "100px", "margin-right": "1000px", "display": "inline", "visibility": "hidden" }).html("Prev").click(function () {
                    currentVideoPosition--;
                    videoPositionChanged();
                    changeVideoDisplay(ytvideos[currentVideoPosition - 1]);                    
                }));
                $(".videoplayer").append($("<button>").prop({ "type": "button", "name": "nextbutton" }).addClass("button").addClass("divider").css({ "width": "100px", "margin-left": "-300px", "display": "inline" }).html("Next").click(function () {
                    currentVideoPosition++;
                    videoPositionChanged();
                    changeVideoDisplay(ytvideos[currentVideoPosition - 1]);
                    
                }));
                
            }            
        }        
    });

    function changeVideoDisplay(video){        
        var iframe = $('[id="videoIframe"]');
        iframe.attr('src', video.videolink + "?&showinfo=0&iv_load_policy=3&controls=2");
        var titleHeader = document.getElementById("titleDisplay");
        titleHeader.innerHTML = "<b>" + video.title + "<b>";        
    }
    function videoPositionChanged(direction) {       
        if (currentVideoPosition == numVideos) {
            $('[name="nextbutton"]').invisible();
            $('[name="prevbutton"]').visible();
        } else if (currentVideoPosition == 1) {
            $('[name="prevbutton"]').invisible();
            $('[name="nextbutton"]').visible();
        } else {
            $('[name="prevbutton"]').visible();
            $('[name="nextbutton"]').visible();
        }                      
    }

    (function ($) {
        $.fn.invisible = function () {
            return this.each(function () {
                $(this).css("visibility", "hidden");
            });
        };
        $.fn.visible = function () {
            return this.each(function () {
                $(this).css("visibility", "visible");
            });
        };
    }(jQuery));

});

