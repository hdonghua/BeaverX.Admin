(function () {
  'use strict';

  var columnIndexCache = null;

  function getDashboardBasePath() {
    var path = window.location.pathname || '';
    var recurringIndex = path.indexOf('/recurring');
    if (recurringIndex > 0) {
      return path.substring(0, recurringIndex);
    }
    return path.replace(/\/$/, '');
  }

  function resolveColumnIndexes() {
    if (columnIndexCache) {
      return columnIndexCache;
    }

    var headers = document.querySelectorAll('.table thead th');
    var cronIndex = -1;
    var idIndex = -1;

    headers.forEach(function (th, index) {
      var text = (th.textContent || '').trim().toLowerCase();
      if (cronIndex === -1 && (text === 'cron' || text.indexOf('cron') >= 0)) {
        cronIndex = index;
      }
      if (
        idIndex === -1 &&
        (text === '编号' ||
          text === 'id' ||
          text === 'common_id' ||
          text.indexOf('id') === 0)
      ) {
        idIndex = index;
      }
    });

    columnIndexCache = {
      cronIndex: cronIndex >= 0 ? cronIndex : 2,
      idIndex: idIndex >= 0 ? idIndex : 1,
    };

    return columnIndexCache;
  }

  function looksLikeCron(text) {
    if (!text) {
      return false;
    }

    var value = text.trim();
    if (!value) {
      return false;
    }

    if (value.indexOf(' ') >= 0) {
      return true;
    }

    return /[*?/,]/.test(value);
  }

  function extractCronText(cell) {
    if (!cell) {
      return '';
    }

    var stored = cell.getAttribute('data-beaverx-cron');
    if (stored) {
      return stored;
    }

    var nodes = cell.querySelectorAll('span, code, .text-muted');
    for (var i = 0; i < nodes.length; i++) {
      var nodeText = (nodes[i].textContent || '').trim();
      if (looksLikeCron(nodeText)) {
        return nodeText;
      }
    }

    var text = (cell.textContent || '').trim();
    if (looksLikeCron(text)) {
      return text;
    }

    var match = text.match(
      /([0-9*\/?,A-Za-z\-]+(?:\s+[0-9*\/?,A-Za-z\-]+)+)/
    );
    return match ? match[1].trim() : '';
  }

  function findJobId(row, idIndex) {
    var checkbox = row.querySelector('input[type="checkbox"][name="jobs[]"]');
    if (checkbox && checkbox.value) {
      return checkbox.value.trim();
    }

    var cells = row.querySelectorAll('td');
    if (idIndex < 0 || idIndex >= cells.length) {
      return '';
    }

    return (cells[idIndex].textContent || '').trim();
  }

  function createEditButton(jobId, cronValue) {
    var button = document.createElement('button');
    button.type = 'button';
    button.className = 'btn btn-xs btn-default beaverx-cron-edit';
    button.title = '编辑 Cron';
    button.innerHTML = '&#9998;';
    button.style.marginLeft = '6px';

    button.addEventListener('click', function (event) {
      event.preventDefault();
      event.stopPropagation();
      openEditor(jobId, cronValue);
    });

    return button;
  }

  function openEditor(jobId, currentCron) {
    var nextCron = window.prompt('编辑 Cron 表达式（5 段或 6 段）', currentCron);
    if (nextCron == null) {
      return;
    }

    nextCron = nextCron.trim();
    if (!nextCron || nextCron === currentCron) {
      return;
    }

    var basePath = getDashboardBasePath();
    var body = new URLSearchParams();
    body.set('id', jobId);
    body.set('cron', nextCron);

    fetch(basePath + '/recurring/cron/update', {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/x-www-form-urlencoded',
        'X-Requested-With': 'XMLHttpRequest',
      },
      body: body.toString(),
      credentials: 'same-origin',
    })
      .then(function (response) {
        return response.json().then(function (payload) {
          if (!response.ok || !payload.success) {
            throw new Error(payload.message || '更新失败');
          }
        });
      })
      .then(function () {
        window.location.reload();
      })
      .catch(function (error) {
        window.alert(error.message || '更新 Cron 失败');
      });
  }

  function enhanceRecurringPage() {
    if ((window.location.pathname || '').indexOf('/recurring') === -1) {
      return;
    }

    var columns = resolveColumnIndexes();
    var rows = document.querySelectorAll('.table tbody tr');

    rows.forEach(function (row) {
      if (row.querySelector('.beaverx-cron-edit')) {
        return;
      }

      var cells = row.querySelectorAll('td');
      if (columns.cronIndex >= cells.length) {
        return;
      }

      var cronCell = cells[columns.cronIndex];
      var jobId = findJobId(row, columns.idIndex);
      var cronValue = extractCronText(cronCell);

      if (!jobId || !cronValue) {
        return;
      }

      cronCell.setAttribute('data-beaverx-cron', cronValue);

      var wrapper = document.createElement('span');
      wrapper.className = 'beaverx-cron-cell';
      wrapper.style.display = 'inline-flex';
      wrapper.style.alignItems = 'center';
      wrapper.style.gap = '6px';

      while (cronCell.firstChild) {
        wrapper.appendChild(cronCell.firstChild);
      }

      wrapper.appendChild(createEditButton(jobId, cronValue));
      cronCell.appendChild(wrapper);
    });
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', enhanceRecurringPage);
  } else {
    enhanceRecurringPage();
  }
})();
