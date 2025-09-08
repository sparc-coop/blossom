let stripeIntegration = {
    elements: null,
    stripe: null,
    paymentElement: null
};

function initialize(element, intent, appearance) {
    element.innerHTML = "";
    if (stripeIntegration.paymentElement) {
        try { stripeIntegration.paymentElement.unmount(); }
        catch (__) { }
    }
    // remove any null values from appearance
    if (appearance) {
        for (const key in appearance.variables) {
            if (appearance.variables[key] === null) {
                delete appearance.variables[key];
            }
        }

        for (const key in appearance.rules) {
            for (const subKey in appearance.rules[key]) {
                if (appearance.rules[key][subKey] === null) {
                    delete appearance.rules[key][subKey];
                }
            }
        }
    }

    stripeIntegration.stripe = Stripe(intent.publishableKey);
    stripeIntegration.elements = stripeIntegration.stripe.elements({
        clientSecret: intent.clientSecret,
        appearance: appearance ?? {
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

    try {
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
    } catch (e) {
        console.error("Error during payment confirmation:", e);
        return { succeeded: false, message: e.message };
    }
}

export { initialize, pay };