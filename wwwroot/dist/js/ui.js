/************************************************************************
 * filename     : ui.js
 * description  : UI js
 * date         : 2025.03.25 JDW
************************************************************************/

$(document).ready(function(){

    /*===== MODAL =====*/ 
    var modal = $('#modalDialog');
    var btn = $("#BtnVoteCounting");
    var span = $(".btn-popup-close");

    btn.on('click', function() {
        modal.show();
        $('body').addClass('modal-open');
    });
    span.on('click', function() {
        modal.fadeOut();
        $('body').removeClass('modal-open');
    });
    
});
