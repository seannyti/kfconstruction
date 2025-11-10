// Broadcast messaging shared JS (Index + Details)
(function(){
    window.confirmDeactivate = function(id, subject){
        if(confirm('Are you sure you want to deactivate the broadcast message "' + subject + '"?\n\nUsers will no longer see this message.')){
            var form = document.getElementById('deactivateForm');
            if(document.getElementById('deactivateId')){
                document.getElementById('deactivateId').value = id;
            }
            if(form){ form.submit(); }
        }
    };

    window.confirmDelete = function(id, subject){
        if(confirm('Are you sure you want to delete the broadcast message "' + subject + '"?\n\nThis action cannot be undone.')){
            var form = document.getElementById('deleteForm');
            if(document.getElementById('deleteId')){
                document.getElementById('deleteId').value = id;
            }
            if(form){ form.submit(); }
        }
    };
})();
