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
    var timeZoneIndex = -1;

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
      if (
        timeZoneIndex === -1 &&
        (text === '时区' ||
          text === 'timezone' ||
          text.indexOf('time zone') >= 0 ||
          text.indexOf('timezone') >= 0)
      ) {
        timeZoneIndex = index;
      }
    });

    columnIndexCache = {
      cronIndex: cronIndex >= 0 ? cronIndex : 2,
      idIndex: idIndex >= 0 ? idIndex : 1,
      timeZoneIndex: timeZoneIndex >= 0 ? timeZoneIndex : 3,
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

    var clone = cell.cloneNode(true);
    var actionNodes = clone.querySelectorAll('.beaverx-job-pause, .beaverx-job-resume');
    actionNodes.forEach(function (node) {
      node.remove();
    });

    var nodes = clone.querySelectorAll('span, code, .text-muted');
    for (var i = 0; i < nodes.length; i++) {
      var nodeText = (nodes[i].textContent || '').trim();
      if (looksLikeCron(nodeText)) {
        return nodeText;
      }
    }

    var text = (clone.textContent || '').trim();
    if (looksLikeCron(text)) {
      return text;
    }

    var match = text.match(
      /([0-9*\/?,A-Za-z\-]+(?:\s+[0-9*\/?,A-Za-z\-]+)+)/
    );
    return match ? match[1].trim() : '';
  }

  function extractTimeZoneText(cell) {
    if (!cell) {
      return '';
    }

    var stored = cell.getAttribute('data-beaverx-timezone');
    if (stored) {
      return stored;
    }

    return (cell.textContent || '').trim();
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

  function getRowJobInfo(row) {
    var columns = resolveColumnIndexes();
    var cells = row.querySelectorAll('td');
    var cronCell = columns.cronIndex < cells.length ? cells[columns.cronIndex] : null;
    var timeZoneCell =
      columns.timeZoneIndex < cells.length ? cells[columns.timeZoneIndex] : null;
    var jobId = findJobId(row, columns.idIndex);
    var cronValue = extractCronText(cronCell);
    var timeZoneValue = extractTimeZoneText(timeZoneCell);

    return {
      jobId: jobId,
      cronValue: cronValue,
      timeZoneValue: timeZoneValue,
      isPaused: isPausedCron(cronValue),
    };
  }

  function getSelectedJobInfo() {
    var checked = document.querySelectorAll(
      'input[type="checkbox"][name="jobs[]"]:checked'
    );

    if (checked.length !== 1) {
      window.alert('请先勾选一条周期性任务');
      return null;
    }

    var row = checked[0].closest('tr');
    if (!row) {
      window.alert('未找到选中的任务行');
      return null;
    }

    var info = getRowJobInfo(row);
    if (!info.jobId) {
      window.alert('无法识别任务编号');
      return null;
    }

    return info;
  }

  function isPausedCron(cronValue) {
    return (cronValue || '').trim() === '0 0 31 2 *';
  }

  function postJson(path, fields) {
    var basePath = getDashboardBasePath();
    var body = new URLSearchParams();
    Object.keys(fields).forEach(function (key) {
      body.set(key, fields[key]);
    });

    return fetch(basePath + path, {
      method: 'POST',
      headers: {
        Accept: 'application/json',
        'Content-Type': 'application/x-www-form-urlencoded',
        'X-Requested-With': 'XMLHttpRequest',
      },
      body: body.toString(),
      credentials: 'same-origin',
    }).then(function (response) {
      return response.json().then(function (payload) {
        if (!response.ok || !payload.success) {
          throw new Error(payload.message || '操作失败');
        }
      });
    });
  }

  function togglePause(jobId, action) {
    postJson('/recurring/pause/toggle', {
      id: jobId,
      action: action,
    })
      .then(function () {
        window.location.reload();
      })
      .catch(function (error) {
        window.alert(error.message || '暂停/恢复任务失败');
      });
  }

  function openTimeZoneEditor(jobId, currentTimeZone) {
    var hint =
      '请输入时区 Id，例如：China Standard Time、UTC、Asia/Shanghai';
    var nextTimeZone = window.prompt(hint, currentTimeZone || 'China Standard Time');
    if (nextTimeZone == null) {
      return;
    }

    nextTimeZone = nextTimeZone.trim();
    if (!nextTimeZone || nextTimeZone === currentTimeZone) {
      return;
    }

    postJson('/recurring/timezone/update', {
      id: jobId,
      timeZoneId: nextTimeZone,
    })
      .then(function () {
        window.location.reload();
      })
      .catch(function (error) {
        window.alert(error.message || '更新时区失败');
      });
  }

  function createPauseButton(jobId, cronValue) {
    var paused = isPausedCron(cronValue);
    var button = document.createElement('button');
    button.type = 'button';
    button.className = paused
      ? 'btn btn-xs btn-success beaverx-job-resume'
      : 'btn btn-xs btn-warning beaverx-job-pause';
    button.title = paused ? '恢复任务' : '暂停任务';
    button.textContent = paused ? '\u25B6' : '\u275A\u275A';
    button.style.marginLeft = '6px';

    button.addEventListener('click', function (event) {
      event.preventDefault();
      event.stopPropagation();

      var action = paused ? 'resume' : 'pause';
      var message = paused
        ? '确认恢复该周期性任务？'
        : '确认暂停该周期性任务？暂停后将不再按 Cron 自动执行。';
      if (!window.confirm(message)) {
        return;
      }

      togglePause(jobId, action);
    });

    return button;
  }

  function createToolbarButton(text, title, className, onClick) {
    var button = document.createElement('button');
    button.type = 'button';
    button.className = className;
    button.textContent = text;
    button.title = title;
    button.style.marginLeft = '5px';
    button.addEventListener('click', function (event) {
      event.preventDefault();
      event.stopPropagation();
      onClick();
    });
    return button;
  }

  function findActionToolbar() {
    var deleteBtn = document.querySelector(
      'button[value="delete"], input[value="delete"], button[name="btn-submit-delete"], input[name="btn-submit-delete"]'
    );
    if (deleteBtn && deleteBtn.parentElement) {
      return deleteBtn.parentElement;
    }

    var triggerBtn = document.querySelector(
      'button[value="trigger"], input[value="trigger"], button[name="btn-submit-trigger"], input[name="btn-submit-trigger"]'
    );
    if (triggerBtn && triggerBtn.parentElement) {
      return triggerBtn.parentElement;
    }

    return (
      document.querySelector('.page-header-bar .btn-toolbar') ||
      document.querySelector('.btn-toolbar')
    );
  }

  function injectToolbarButtons() {
    if (document.querySelector('.beaverx-toolbar-actions')) {
      return;
    }

    var toolbar = findActionToolbar();
    if (!toolbar) {
      return;
    }

    var container = document.createElement('span');
    container.className = 'beaverx-toolbar-actions';
    container.style.display = 'inline-block';
    container.style.marginLeft = '5px';

    container.appendChild(
      createToolbarButton(
        '编辑 Cron',
        '编辑选中任务的 Cron 表达式',
        'btn btn-default beaverx-toolbar-edit-cron',
        function () {
          var info = getSelectedJobInfo();
          if (!info) {
            return;
          }

          var promptMessage = info.isPaused
            ? '该任务已暂停，请输入恢复后使用的 Cron 表达式（5 段或 6 段）'
            : '编辑 Cron 表达式（5 段或 6 段）';
          var initialCron = info.isPaused ? '0 0 * * *' : info.cronValue;
          var nextCron = window.prompt(promptMessage, initialCron);
          if (nextCron == null) {
            return;
          }

          nextCron = nextCron.trim();
          if (!nextCron || (!info.isPaused && nextCron === info.cronValue)) {
            return;
          }

          postJson('/recurring/cron/update', {
            id: info.jobId,
            cron: nextCron,
          })
            .then(function () {
              window.location.reload();
            })
            .catch(function (error) {
              window.alert(error.message || '更新 Cron 失败');
            });
        }
      )
    );

    container.appendChild(
      createToolbarButton(
        '修改时区',
        '修改选中任务的时区',
        'btn btn-default beaverx-toolbar-edit-timezone',
        function () {
          var info = getSelectedJobInfo();
          if (!info) {
            return;
          }

          openTimeZoneEditor(info.jobId, info.timeZoneValue);
        }
      )
    );

    toolbar.appendChild(container);
  }

  function enhanceRecurringRows() {
    var columns = resolveColumnIndexes();
    var rows = document.querySelectorAll('.table tbody tr');

    rows.forEach(function (row) {
      if (row.getAttribute('data-beaverx-enhanced') === '1') {
        return;
      }

      var cells = row.querySelectorAll('td');
      if (columns.cronIndex >= cells.length) {
        return;
      }

      var cronCell = cells[columns.cronIndex];
      var timeZoneCell =
        columns.timeZoneIndex < cells.length ? cells[columns.timeZoneIndex] : null;
      var jobId = findJobId(row, columns.idIndex);
      var cronValue = extractCronText(cronCell);
      var timeZoneValue = extractTimeZoneText(timeZoneCell);

      if (!jobId || !cronValue) {
        return;
      }

      row.setAttribute('data-beaverx-enhanced', '1');
      row.setAttribute('data-beaverx-job-id', jobId);
      row.setAttribute('data-beaverx-cron', cronValue);
      if (timeZoneValue) {
        row.setAttribute('data-beaverx-timezone', timeZoneValue);
      }

      cronCell.setAttribute('data-beaverx-cron', cronValue);
      if (timeZoneCell && timeZoneValue) {
        timeZoneCell.setAttribute('data-beaverx-timezone', timeZoneValue);
      }

      if (row.querySelector('.beaverx-job-pause, .beaverx-job-resume')) {
        return;
      }

      var wrapper = document.createElement('span');
      wrapper.className = 'beaverx-cron-cell';
      wrapper.style.display = 'inline-flex';
      wrapper.style.alignItems = 'center';
      wrapper.style.gap = '6px';

      while (cronCell.firstChild) {
        wrapper.appendChild(cronCell.firstChild);
      }

      wrapper.appendChild(createPauseButton(jobId, cronValue));
      cronCell.appendChild(wrapper);
    });
  }

  function enhanceRecurringPage() {
    if ((window.location.pathname || '').indexOf('/recurring') === -1) {
      return;
    }

    injectToolbarButtons();
    enhanceRecurringRows();
  }

  if (document.readyState === 'loading') {
    document.addEventListener('DOMContentLoaded', enhanceRecurringPage);
  } else {
    enhanceRecurringPage();
  }
})();
