window.stripeIntegration = {
    initPaymentForm: async function (clientSecret, publishableKey) {
        console.log("initPaymentForm called with:", clientSecret, publishableKey);
        // 1) Load Stripe.js using your publishable key
        const stripe = Stripe(publishableKey);
        console.log("Stripe initialized:");
        console.log(stripe);
        // 2) Create an Elements instance
        var elements = stripe.elements({
            clientSecret: clientSecret,
        });
        console.log("elements initialized:");
        console.log(elements);

        // 3) Create a Payment Element (Stripe's recommended approach) 
        //    or use the older Card Element if you prefer
        var paymentElement = elements.create("payment");

        paymentElement.mount("#payment-element");

        // 4) Attach a handler to the form to confirm the payment
        const form = document.getElementById("payment-form");

        form.addEventListener("submit", async (event) => {
            event.preventDefault();

            // 1) Submit the Payment Element to collect data
            const { error: submitError } = await elements.submit();
            if (submitError) {
                // show the error to the user
                document.getElementById("error-message").textContent = submitError.message;
                return;
            }

            // 2) (Optional) Do any async checks or server calls here
            //    e.g., verify cart, check inventory, etc.

            // 3) Now confirm the PaymentIntent
            const { error: confirmError } = await stripe.confirmPayment({
                clientSecret: clientSecret,
                elements,
                confirmParams: {
                    return_url: "https://localhost:44373/payment-success"
                }
            });

            if (confirmError) {
                document.getElementById("error-message").textContent = confirmError.message;
            }
        });
    }
};
