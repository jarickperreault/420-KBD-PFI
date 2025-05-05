$(document).ready(function () {
    $('.phone').mask('(999) 999-9999');
    $('.phoneExt').mask('(999) 999-9999 poste 99999');
    $('.zipcode').mask('a9a 9a9');
    $(".datepicker").datepicker({
        dateFormat: 'yy-mm-dd',
        changeMonth: true,
        changeYear: true,
        //yearRange: "-100:-15",
        dayNamesMin: ["Dim", "Lun", "Mar", "Mer", "Jeu", "Ven", "Sam"],
        monthNamesShort: ["Janv.", "Févr.", "Mars", "Avril", "Mai", "Juin", "Juil.", "Août", "Sept.", "Oct.", "Nov.", "Déc."]
    });

    /*Filter unicode hack */
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
        $(e.target).next().attr("src", "/Images_Data/Loading_icon.gif")
        $.ajax({
            url: "/CountryFlag/get?countryCode=" + $(e.target).val(),
            datatype: "application/json",
            success: json => { $(e.target).next().attr("src", json); }
        });

    })
    SummaryHandling();
})


function SummaryHandling() {

    $('summary').attr('title', 'Utilisez ctrl-clic pour développer/réduire');
    $('summary').off();
    // Toggle collapse uncollapse details
    $('summary').on('click', function (e) {
        if (e.ctrlKey) {
            if ($(this).parent().attr('open') != undefined) {
                $('details').removeAttr('open');
                e.preventDefault();
            }
            else {
                $('details').prop('open', true);
                e.preventDefault();
            }
        }
    })
}

$(".submitCmd").click(function () {
    $("form").submit();
})
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
        // don't navigate away from the field on tab when selecting an item
        .bind("keydown", function (event) {
            if (event.keyCode === $.ui.keyCode.TAB && $(this).data("ui-autocomplete").menu.active) {
                event.preventDefault();
            }
        })
        .autocomplete({
            minLength: 1,
            source: function (request, response) {
                // delegate back to autocomplete, but extract the last term
                response($.ui.autocomplete.filter(words, extractLast(request.term)));
            },
            focus: function () {
                // prevent value inserted on focus
                return false;
            },
            select: function (event, ui) {
                var terms = split(this.value);
                // remove the current input
                terms.pop();
                // add the selected item
                terms.push(ui.item.value);
                // add placeholder to get the comma-and-space at the end
                terms.push("");
                this.value = RemoveExtra(terms, ",").join(" ");
                return false;
            }
        });
}

function ajaxActionCall(actionLink) {
    // Ajax Action Call to actionLink
    $.ajax({
        url: actionLink,
        method: 'GET',
        success: (data) => {
            console.log("Result: " + data);
        }
    });
}
function commentsSectionAttachEvents() {

    $(".abortEditCommentCmd").off();
    $(".abortEditCommentCmd").click(function (event) {
        event.preventDefault();
        commentsSection.restart();
        let id = $(this).attr("cmdCommentId");
        $("#" + id).hide();
        $("[commentId=" + id + "]").show();
    })
    $(".updateCommentCmd").off();
    $(".updateCommentCmd").click(function (event) {
        event.preventDefault();
        let commentId = $(this).attr("cmdCommentId");
        let commentText = $(this).parent().parent().find("textarea").val();
        commentsSection.postCommand("/Photos/UpdateComment", { commentId, commentText });
        commentsSection.restart();
    })
    $(".deleteCommentCmd").off()
    $(".deleteCommentCmd").click(function (event) {
        event.preventDefault();
        commentsSection.pause();
        let commentId = $(this).attr("cmdCommentId");
        commentsSection.confirmedCommand("Effacer?", `/Photos/DeleteComment?id=${commentId}`);
        commentsSection.restart();
    })
}
function detailsPanelAttachEvents() {
    $("#addRemoveLikeCmd").off();
    $("#addRemoveLikeCmd").click(function (e) {
        detailsPanel.command("/Photos/TogglePhotoLike/" + $(this).attr("photoId"));
        e.preventDefault();
    });
}
function commentsSectionAttachEvents() {
    $("#commentDetails .fa-thumbs-up").off();
    $("#commentDetails .fa-thumbs-up").click(function (e) {
        commentsSection.command("/Photos/ToggleCommentLike/" + $(this).attr("commentId"));
        e.preventDefault();
    });
    $(".editCommentLayout").hide();
    $(".newCommentLayout").hide();
    $(".responseLayout").hide();

    $(".newCommentCmd").off();
    $(".newCommentCmd").click(function () {
        $("#commentDetails").prop('open', true);

        commentsSection.pause();
        let parentId = $(this).attr("parentId");
        $(`[createCommentparentId="${parentId}"]`).show();
        $(`[createCommentparentId="${parentId}"] > textarea`).val("");

        $('#content').animate({
            scrollTop: $(`[createCommentparentId="${parentId}"] > textarea`).offset().top
        }, scrollAnimationSpeed)
            .promise()
            .done(() => { $(`[createCommentparentId="${parentId}"] > textarea`).focus(); });
    })
    $(".abortCreateCommentCmd").off();
    $(".abortCreateCommentCmd").click(function (event) {
        event.preventDefault();
        commentsSection.restart();
        let parentId = $(this).attr("parentId");
        $(`[createCommentparentId="${parentId}"]`).hide();
    })

    $(".createCommentCmd").off();
    $(".createCommentCmd").click(function (event) {
        event.preventDefault();
        let parentId = $(this).attr("parentId");
        let commentText = $(this).parent().parent().find("textarea").val();
        commentsSection.postCommand("/Photos/CreateComment", { parentId, commentText });
        commentsSection.restart();
    })
    $(".CreateResponseCommentCmd").off();
    $(".CreateResponseCommentCmd").click(function () {
        commentsSection.pause();
        let parentId = $(this).attr("parentId");
        console.log($(`[createResponseparentId="${parentId}"]`)[0])
        $(`[createResponseparentId="${parentId}"]`).show();
        $(`[createResponseparentId="${parentId}"] > textarea`).val("");
        $('#content').animate({
            scrollTop: $(`[createResponseparentId="${parentId}"] > textarea`).offset().top
        }, scrollAnimationSpeed)
            .promise()
            .done(() => { $(`[createResponseparentId="${parentId}"] > textarea`).focus(); });

    })
    $(".abortCreateResponseCmd").off();
    $(".abortCreateResponseCmd").click(function (event) {
        event.preventDefault();
        commentsSection.restart();
        let parentId = $(this).attr("parentId");
        $(`[createResponseparentId="${parentId}"]`).hide();
    })

    $(".createResponseCmd").off();
    $(".createResponseCmd").click(function (event) {
        event.preventDefault();
        let parentId = $(this).attr("parentId");
        let commentText = $(this).parent().parent().find("textarea").val();
        commentsSection.postCommand("/Photos/CreateComment", { parentId, commentText });
        commentsSection.restart();
    })
    $(".editCommentCmd").off();
    $(".editCommentCmd").click(function () {
        commentsSection.pause();
        let id = $(this).attr("commentId");
        $("#" + id).show();
        $("#" + id + "> textarea").focus();
        $("[commentId=" + id + "]").hide();
    })

    $(".abortEditCommentCmd").off();
    $(".abortEditCommentCmd").click(function (event) {
        event.preventDefault();
        commentsSection.restart();
        let id = $(this).attr("cmdCommentId");
        $("#" + id).hide();
        $("[commentId=" + id + "]").show();
    })

    $(".updateCommentCmd").off();
    $(".updateCommentCmd").click(function (event) {
        event.preventDefault();
        let commentId = $(this).attr("cmdCommentId");
        let commentText = $(this).parent().parent().find("textarea").val();
        commentsSection.postCommand("/Photos/UpdateComment", { commentId, commentText });
        commentsSection.restart();
    })

    $(".deleteCommentCmd").off()
    $(".deleteCommentCmd").click(function (event) {
        event.preventDefault();
        commentsSection.pause();
        let commentId = $(this).attr("cmdCommentId");
        commentsSection.confirmedCommand("Effacer?", `/Photos/DeleteComment?id=${commentId}`);
        commentsSection.restart();
    })
}


