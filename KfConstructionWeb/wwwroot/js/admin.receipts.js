// Receipts management JS
(function(){
    window.confirmDeleteReceipt = function(id){
        if(confirm('Are you sure you want to delete this receipt?\n\nThis action will soft-delete the receipt with a 30-day grace period before permanent deletion.')){
            const form = document.getElementById('deleteForm');
            if(form && window.__receipts && window.__receipts.deleteBaseUrl){
                form.action = window.__receipts.deleteBaseUrl + '/' + id;
                form.submit();
            }
        }
    };

    window.printReceipts = function(){
        const startDate = document.getElementById('printStartDate').value;
        const endDate = document.getElementById('printEndDate').value;

        if(!startDate || !endDate){
            alert('Please select both start and end dates.');
            return;
        }
        if(new Date(startDate) > new Date(endDate)){
            alert('Start date must be before end date.');
            return;
        }
        if(window.__receipts && window.__receipts.printBaseUrl){
            const url = window.__receipts.printBaseUrl + '?startDate=' + encodeURIComponent(startDate) + '&endDate=' + encodeURIComponent(endDate);
            window.open(url, '_blank');
            const modalEl = document.getElementById('printReceiptsModal');
            if(modalEl){
                const modal = bootstrap.Modal.getInstance(modalEl);
                if(modal){ modal.hide(); }
            }
        }
    };
})();
