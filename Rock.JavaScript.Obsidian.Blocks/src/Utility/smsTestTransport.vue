<!-- Copyright by the Spark Development Network; Licensed under the Rock Community License -->
<template>
    <div class="d-flex">
        <div class="mr-2">
            <TextBox v-model="fromNumber" label="From" />
        </div>
        <div class="mr-2">
            <TextBox v-model="toNumber" label="To" />
        </div>
        <div class="flex-grow-1 mr-2">
            <TextBox v-model="body" label="Body" />
        </div>
        <div style="margin-top: 26px;">
            <RockButton :btnType="primaryButtonType" @click="onSubmitMessage">Submit</RockButton>
        </div>
    </div>

    <div>
        <div v-for="msg in messages" :class="getMessageClass(msg)">
            From: {{ msg.fromNumber }}<br />
            To: {{ msg.toNumber }}<br />
            <br />
            {{ msg.body }}
        </div>
    </div>
</template>

<style scoped>
.message-bubble {
    width: 35%;
    padding: 8px;
    margin: 0px 0px 12px auto;
    border-radius: 8px;
    background-color: rgb(16, 153, 244);
    color: white;
}

.message-bubble.message-incoming {
    margin: 0px auto 12px 0px;
    background: rgb(49, 176, 69);
    color: white;
}
</style>

<script setup lang="ts">
    import TextBox from "@Obsidian/Controls/textBox";
    import RockButton from "@Obsidian/Controls/rockButton";
    import { BtnType } from "@Obsidian/Enums/Controls/buttonOptions";
    import { ref } from "vue";
    import { getTopic, ITopic } from "@Obsidian/Utility/realTime";

    type SmsMessage = {
        fromNumber?: string | null;
        toNumber?: string | null;
        body?: string | null;
        attachmentUrls?: string[] | null
    };

    type TrackedSmsMessage = SmsMessage & {
        incoming: boolean;
    };

    interface ITestCommunicationTransportTopic {
        messageReceived(message: SmsMessage): Promise<void>;
    }

    // #region Values

    const realTimeTopic = ref<ITopic<ITestCommunicationTransportTopic> | null>(null);
    const messages = ref<TrackedSmsMessage[]>([]);
    const fromNumber = ref("");
    const toNumber = ref("");
    const body = ref("");
    const primaryButtonType = BtnType.Primary;

    // #endregion

    // #region Computed Values

    // #endregion

    // #region Functions

    async function startRealTime(): Promise<void> {
        const topic = await getTopic<ITestCommunicationTransportTopic>("Rock.RealTime.Topics.TestCommunicationTransportTopic");

        topic.on("smsMessageSent", onSmsMessageSent);

        realTimeTopic.value = topic;
    }

    function getMessageClass(message: TrackedSmsMessage): string[] {
        if (message.incoming) {
            return ["message-bubble", "message-incoming"];
        }
        else {
            return ["message-bubble", "message-outgoing"];
        }
    }

    async function onSubmitMessage(): Promise<void> {
        await realTimeTopic.value?.server.messageReceived({
            fromNumber: fromNumber.value,
            toNumber: toNumber.value,
            body: body.value
        });

        messages.value.splice(0, 0, {
            fromNumber: fromNumber.value,
            toNumber: toNumber.value,
            body: body.value,
            incoming: true
        });

        body.value = "";
    }

    // #endregion

    // #region Event Handlers

    function onSmsMessageSent(message: SmsMessage): void {
        messages.value.splice(0, 0, {
            ...message,
            incoming: false
        });
        console.log("Got message", message);
    }

    // #endregion

    startRealTime();
</script>
