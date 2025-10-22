/************************************************************************
 * filename     : sub.js
 * description  : sub js
 * date         : 2025.04.08 JDW
************************************************************************/

$(document).ready(function(){

    //서브 탭 설정
    $('.nav-tabs').scrollTabs({
        // Pixel width for the scroll button on the left side.
        left_arrow_size: 30,
        // Pixel width for the scroll button on the right side.
        right_arrow_size: 30,
    });

    //datepicker
    //https://www.jqueryscript.net/time-clock/Highly-Customizable-jQuery-Datepicker-Plugin-Datepicker.html
    $('[data-toggle="datepicker"]').datepicker({
        format:'yyyy-mm-dd',
        language : "ko-KR"
    });

    //상단 필터 토글 버튼
    $('#favorites-toggle-switch').on('change', function () {
        if ($(this).is(':checked')) {
            $('.content-filter').slideDown();
        } else {
            $('.content-filter').slideUp();
        }
    });


    //상단 탭 스크롤 효과
    const container = document.querySelector('.navbar-nav-tab');
    if (container != null) {
        const wrapper = container.querySelector('.main-menu');
        function updateShadows() {
            const scrollLeft = wrapper.scrollLeft;
            const scrollWidth = wrapper.scrollWidth;
            const clientWidth = wrapper.clientWidth;

            const showLeft = scrollLeft > 0;
            const showRight = scrollLeft + clientWidth < scrollWidth - 1;

            container.classList.toggle('scroll-shadow-left', showLeft);
            container.classList.toggle('scroll-shadow-right', showRight);
        }

        wrapper.addEventListener('scroll', updateShadows);
        window.addEventListener('resize', updateShadows);
        updateShadows(); // 초기 실행
    }
    


    //즐겨찾기 설정
    const toggleBtn = document.getElementById('btn-toggle-favorites');
    const switchEl = document.getElementById('favoritesSwitch');
    const slidePanel = document.getElementById('favorites-sidebar');
    const closeBtn = document.getElementById('btn-close-favorites');

    if (toggleBtn != null) {
        toggleBtn.addEventListener('click', () => {
            switchEl.classList.toggle('on');
            slidePanel.classList.toggle('open');
        });
    }

    if (closeBtn != null) {
        closeBtn.addEventListener('click', () => {
            slidePanel.classList.remove('open');
            switchEl.classList.remove('on');
        });
    }

});

//상단 고정 설정
$(window).scroll(function(){
    if ($(window).scrollTop() >= 100) {
        $('.main-header').addClass('fixed-header');
    }
    else {
        $('.main-header').removeClass('fixed-header');
    }
});

//iframe 내부 상단 고정 설정
jQuery(document).ready(function(){
    var bodyOffset = jQuery('body').offset();
    jQuery(window).scroll(function() {
        if (jQuery(document).scrollTop() > bodyOffset.top) {
            jQuery('.content-wrapper').addClass('header-scroll');
        } else {
            jQuery('.content-wrapper').removeClass('header-scroll');
        }
    });
});



