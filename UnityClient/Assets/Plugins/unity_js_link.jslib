mergeInto(LibraryManager.library, {

  SelectPID: function (pid) {
    window.alert(UTF8ToString(pid));
  },

  DownloadFile: function(filename, filedata) {
    downloadFile(filename, filedata);
  }

});