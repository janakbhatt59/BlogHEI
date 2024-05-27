(function ($) {
    $.fn.pagination = function (p) {
        var $self = $(this);

        var Pagination = {
            init: function () {
                p.pageNo = parseInt(p.pageNo);
                p.itemsPerPage = parseInt(p.itemsPerPage);
                p.pagePerDisplay = parseInt(p.pagePerDisplay);
                p.totalNextPages = parseInt(p.totalNextPages);
                p.totalRecords = parseInt(p.totalRecords);

                Pagination.bindPager();
            },
            bindPager: function () {
                var html = '';
                var start = Math.floor((p.pageNo - 1) / p.pagePerDisplay) * p.pagePerDisplay + 1;
                var end = start + (p.pagePerDisplay - 1);
                var totalPages = Math.ceil(p.totalRecords / p.itemsPerPage);
                if ((end - p.pageNo) > p.totalNextPages) end = p.pageNo + p.totalNextPages;

                var prev = p.pageNo - 1;
                var next = p.pageNo + 1;

                if (prev > 0) {
                    html += '<li class="page-item"><a class="page-link" href="javascript:void(0);" data-page="1">First</a></li>';
                    html += '<li class="page-item"><a class="page-link" href="javascript:void(0);" data-page="' + prev + '">Prev</a></li>';
                }

                for (var i = start; i <= end; i++) {
                    html += '<li class="page-item ' + (i == p.pageNo ? 'active' : '') + '"><a class="page-link" href="javascript:void(0);" data-page="' + i + '">' + i + '</a></li>';
                }

                if (p.totalNextPages > 0) {
                    html += '<li class="page-item"><a class="page-link" href="javascript:void(0);" data-page="' + next + '">Next</a></li>';
                }

                if (totalPages > end) {
                    html += '<li class="page-item"><a class="page-link" href="javascript:void(0);" data-page="' + totalPages + '">Last</a></li>';
                }

                $self.html('<nav><ul class="pagination float-end">' + html + '</ul></nav>');

                $self.find('[data-page]').on('click', function () {
                    var page = parseInt($(this).attr('data-page'));
                    p.callback(page);
                });
            }
        }

        Pagination.init();
    }
})(jQuery);
