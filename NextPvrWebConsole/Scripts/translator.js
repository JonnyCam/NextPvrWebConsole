﻿if (language == null)
    return;


$.each($('[data-lang]'), function (i, ele) {
    $(this).text($.i18n._($(this).attr('data-lang')));
});