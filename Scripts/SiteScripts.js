$(document).ready(function () {
    console.log("SiteScripts.js loaded");

    $('.phone').mask('(999) 999-9999');
    $('.phoneExt').mask('(999) 999-9999 poste 99999');
    $('.zipcode').mask('a9a 9a9');
    $(".datepicker").datepicker({
        dateFormat: 'yy-mm-dd',
        changeMonth: true,
        changeYear: true,
        dayNamesMin: ["Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam"],
        monthNamesShort: ["Janv.", "Févr.", "Mars", "Avril", "Mai", "Juin", "Juil.", "Août", "Sept.", "Oct.", "Nov.", "Déc."]
    });

    /* Filter unicode hack */
    $(":input").change(function () {
        try {
            let r = $(this).val().replace(/[^\x00-\xFF]/g, "");
            $(this).val(r);
        } catch (e) { }
    });
    $("textarea").change(function () {
        try {
            let r = $(this).val().replace(/[^\x00-\xFF]/g, "");
            $(this).val(r);
        } catch (e) { }
    });

    $(".countrySelect").change((e) => {
        $(e.target).next().attr("src", "/Images_Data/Loading_icon.gif");
        $.ajax({
            url: "/CountryFlag/get?countryCode=" + $(e.target).val(),
            datatype: "application/json",
            success: json => { $(e.target).next().attr("src", json); }
        });
    });

    SummaryHandling();

    // AutoRefreshedPanel implementation
    function AutoRefreshedPanel(elementId, url, interval, callback) {
        this.elementId = elementId;
        this.url = url;
        this.interval = interval * 1000;
        this.isPaused = false;
        var intervalId = null;
        this.restart = function () {
            if (!this.isPaused && $("#" + elementId).length) {
                console.log("Refreshing comments for " + elementId);
                $("#" + elementId).load(url, callback);
            }
        };
        this.postCommand = function (cmdUrl, data) {
            $.post(cmdUrl, data, function (response) {
                console.log("Post command response:", response);
                this.restart();
            }.bind(this)).fail(function (xhr, status, error) {
                console.error("Post command failed:", status, error);
            });
        };
        this.confirmedCommand = function (msg, cmdUrl) {
            if (confirm(msg)) {
                $.post(cmdUrl, this.restart).fail(function (xhr, status, error) {
                    console.error("Confirmed command failed:", status, error);
                });
            }
        };
        this.pause = function () {
            this.isPaused = true;
            console.log("Paused refresh for " + elementId);
        };
        this.resume = function () {
            this.isPaused = false;
            this.restart();
            console.log("Resumed refresh for " + elementId);
        };
        if ($("#" + elementId).length) {
            intervalId = setInterval(this.restart, this.interval);
            this.restart();
        } else {
            console.log("Skipping AutoRefreshedPanel for " + elementId + ": element not found");
        }
    }

    // Initialize comments section only if #commentsSection exists
    if ($("#commentsSection").length) {
        var commentsSection = new AutoRefreshedPanel("commentsSection", "/Photos/GetComments", 30, commentsSectionAttachments);
    }

    function commentsSectionAttachments() {
        // Use event delegation for dynamically loaded elements
        $(document).off("click", ".editCommentCmd").on("click", ".editCommentCmd", function (event) {
            event.preventDefault();
            console.log("Edit comment clicked");
            let id = $(this).attr("cmdCommentId");
            let originalText = $("[commentId=" + id + "]").text();
            $("#" + id).val(originalText).show();
            $("[commentId=" + id + "]").hide();
        });

        $(document).off("click", ".updateCommentCmd").on("click", ".updateCommentCmd", function (event) {
            event.preventDefault();
            console.log("Update comment clicked");
            let commentId = $(this).attr("cmdCommentId");
            let commentText = $(this).parent().parent().find("textarea").val();
            commentsSection.postCommand("/Photos/UpdateComment", { commentId, commentText });
        });

        $(document).off("click", ".abortEditCommentCmd").on("click", ".abortEditCommentCmd", function (event) {
            event.preventDefault();
            console.log("Abort edit comment clicked");
            let id = $(this).attr("cmdCommentId");
            $("#" + id).hide();
            $("[commentId=" + id + "]").show();
        });

        $(document).off("click", ".deleteCommentCmd").on("click", ".deleteCommentCmd", function (event) {
            event.preventDefault();
            console.log("Delete comment clicked");
            let commentId = $(this).attr("cmdCommentId");
            commentsSection.confirmedCommand("Effacer?", `/Photos/DeleteComment?id=${commentId}`);
        });

        $(document).off("click", ".CreateResponseCommentCmd").on("click", ".CreateResponseCommentCmd", function (event) {
            event.preventDefault();
            console.log("Create response comment clicked");
            let parentId = $(this).attr("parentId");
            $(".responseLayout[createResponseparentId='" + parentId + "']").show();
        });

        $(document).off("click", ".createResponseCmd").on("click", ".createResponseCmd", function (event) {
            event.preventDefault();
            console.log("Create response submitted");
            let parentId = $(this).attr("parentId");
            let photoId = $(".responseLayout[createResponseparentId='" + parentId + "']").attr("photoId");
            let text = $(".responseLayout[createResponseparentId='" + parentId + "'] textarea").val();
            if (!text.trim()) {
                alert("Comment cannot be empty");
                return;
            }
            commentsSection.postCommand("/Photos/CreateComment", { photoId: photoId, parentId: parentId, text: text });
            $(".responseLayout[createResponseparentId='" + parentId + "'] textarea").val("");
            $(".responseLayout[createResponseparentId='" + parentId + "']").hide();
        });

        $(document).off("click", ".abortCreateResponseCmd").on("click", ".abortCreateResponseCmd", function (event) {
            event.preventDefault();
            console.log("Abort create response clicked");
            let parentId = $(this).attr("parentId");
            $(".responseLayout[createResponseparentId='" + parentId + "']").hide();
        });

        $(document).off("click", "#pauseRefresh").on("click", "#pauseRefresh", function (event) {
            event.preventDefault();
            console.log("Pause/Resume clicked");
            if (commentsSection.isPaused) {
                commentsSection.resume();
                $(this).removeClass("fa-play").addClass("fa-pause").attr("title", "Pause auto-refresh");
            } else {
                commentsSection.pause();
                $(this).removeClass("fa-pause").addClass("fa-play").attr("title", "Resume auto-refresh");
            }
        });
    }

    function SummaryHandling() {
        $('summary').attr('title', 'Utilisez ctrl-clic pour développer/réduire');
        $('summary').off();
        $('summary').on('click', function (e) {
            if (e.ctrlKey) {
                if ($(this).parent().attr('open') != undefined) {
                    $('details').removeAttr('open');
                    e.preventDefault();
                } else {
                    $('details').prop('open', true);
                    e.preventDefault();
                }
            }
        });
    }

    $(".submitCmd").click(function () {
        $("form").submit();
    });

    function InstallAutoComplete(targetId, words) {
        function split(val) {
            return val.split(/ \s*/);
        }

        function RemoveExtra(str, extra) {
            var extraLength = extra.length;
            var lastExtraIndex = str.lastIndexOf(extra);
            if ((lastExtraIndex + extraLength) == str.length)
                str = str.substring(0, str.length - extraLength);
            return str;
        }

        function extractLast(term) {
            return split(term).pop();
        }

        $("#" + targetId)
            .bind("keydown", function (event) {
                if (event.keyCode === $.ui.keyCode.TAB && $(this).data("ui-autocomplete").menu.active) {
                    event.preventDefault();
                }
            })
            .autocomplete({
                minLength: 1,
                source: function (request, response) {
                    response($.ui.autocomplete.filter(words, extractLast(request.term)));
                },
                focus: function () {
                    return false;
                },
                select: function (event, ui) {
                    var terms = split(this.value);
                    terms.pop();
                    terms.push(ui.item.value);
                    terms.push("");
                    this.value = RemoveExtra(terms, ",").join(" ");
                    return false;
                }
            });
    }

    function ajaxActionCall(actionLink) {
        $.ajax({
            url: actionLink,
            method: 'GET',
            success: (data) => {
                console.log("Result: " + data);
            }
        });
    }
}