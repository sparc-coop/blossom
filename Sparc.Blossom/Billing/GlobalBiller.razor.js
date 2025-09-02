let stripeIntegration = {
    elements: null,
    stripe: null,
    paymentElement: null
};

function initialize(element, intent) {
    element.innerHTML = "";
    if (stripeIntegration.paymentElement) {
        try { stripeIntegration.paymentElement.unmount(); }
        catch (__) { }
    }
    console.log("initPaymentForm called with:", intent, element);

    stripeIntegration.stripe = Stripe(intent.publishableKey);
    stripeIntegration.elements = stripeIntegration.stripe.elements({
        clientSecret: intent.clientSecret,
        appearance: {
            theme: 'flat',

            variables: {
                colorPrimary: '#3f256b',
                colorText: '#3f256b',
                colorBackground: '#ffffff',
                borderRadius: '26px',
                spacingUnit: '4px'
            },

            rules: {
                '.Label': {
                    fontSize: '13px',
                    fontWeight: 500
                },
                '.Input': {
                    border: '1px solid #DABEFA'
                },
                '.Tab': {
                    borderRadius: '16px'
                }

            }
        }
    });

    const paymentElement = stripeIntegration.elements.create("payment");
    paymentElement.mount('#' + element.id);
}

async function pay(returnUrl) {
    let elements = stripeIntegration.elements;

    const { error: submitError } = await elements.submit();
    if (submitError) {
        console.error("Elements submit error:", submitError);

        return {
            succeeded: false,
            message: submitError.message
        };
    }

    console.log("Elements submitted successfully. Confirming payment...");

    const { error, paymentIntent } = await stripeIntegration.stripe.confirmPayment({
        elements,
        confirmParams: { return_url: returnUrl },
        redirect: "if_required"
    });

    if (error) {
        return { succeeded: false, message: error.message };
    }

    return {
        succeeded: paymentIntent?.status === "succeeded",
        message: paymentIntent?.status === "succeeded" ? "Payment succeeded" : "Payment not completed"
    };
}

export { initialize, pay };