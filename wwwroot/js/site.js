// // This is pure JavaScript for SignalR
// var connection = new signalR.HubConnectionBuilder()
//     .withUrl("/notificationHub")
//     .build();

// connection.on("ReceiveMessage", function (user, message) {
//     // This will create a popup when the server sends a message
//     alert(user + ": " + message);
// });

// connection.start().catch(function (err) {
//     return console.error(err.toString());
// });

$(document).ready(function () {
    $('.btn-add-to-cart').click(function () {
        var productId = $(this).data('id'); 
        var button = $(this); 

        $.ajax({
            url: '/Frontend/AddToCart',
            type: 'POST',
            
            data: { productId: productId }, 
            success: function (response) {
                button.text("ADDED! ✓").addClass("btn-success").removeClass("btn-warning");
                
                setTimeout(function() {
                    button.text("ADD TO CART").addClass("btn-warning").removeClass("btn-success");
                }, 2000);

                var currentCount = parseInt($('#cart-count').text()) || 0;
                $('#cart-count').text(currentCount + 1);
            },
            error: function(xhr) {
                if (xhr.status === 401) {
                    window.location.href = '/Account/Login'; 
                    alert("Please login first!");
                }
            }
        });
    });
});