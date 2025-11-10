// Backup management page JS
(function(){
    window.confirmBackup = function(){
        if(confirm('Are you sure you want to create a database backup? This may take several minutes and temporarily affect performance.')){
            var form = document.getElementById('createBackupForm');
            if(form){ form.submit(); }
        }
    };

    window.confirmRestore = function(fileName){
        if(confirm('WARNING: Restoring a backup will REPLACE the current database with the backup data. All current data will be lost!\n\nAre you absolutely sure you want to restore from: ' + fileName + '?')){
            if(confirm('This action CANNOT be undone. Please confirm once more.')){
                var input = document.getElementById('restoreFileName');
                var form = document.getElementById('restoreForm');
                if(input){ input.value = fileName; }
                if(form){ form.submit(); }
            }
        }
    };

    window.confirmDelete = function(fileName){
        if(confirm('Are you sure you want to delete the backup file: ' + fileName + '?\n\nThis action cannot be undone.')){
            var input = document.getElementById('deleteFileName');
            var form = document.getElementById('deleteForm');
            if(input){ input.value = fileName; }
            if(form){ form.submit(); }
        }
    };
})();
