// <copyright>
// Copyright by the Spark Development Network
//
// Licensed under the Rock Community License (the "License");
// you may not use this file except in compliance with the License.
// You may obtain a copy of the License at
//
// http://www.rockrms.com/license
//
// Unless required by applicable law or agreed to in writing, software
// distributed under the License is distributed on an "AS IS" BASIS,
// WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
// See the License for the specific language governing permissions and
// limitations under the License.
// </copyright>
//

import { useSecurityGrantToken } from "@Obsidian/Utility/block";
import { updateRefValue } from "@Obsidian/Utility/component";
import { ListItemBag } from "@Obsidian/ViewModels/Utility/listItemBag";
import { defineComponent, PropType, ref, watch } from "vue";
import { MergeTemplateTreeItemProvider } from "@Obsidian/Utility/treeItemProviders";
import { MergeTemplateOwnership } from "@Obsidian/Enums/Controls/mergeTemplateOwnership";
import TreeItemPicker from "./treeItemPicker.obs";

export default defineComponent({
    name: "MergeTemplatePicker",

    components: {
        TreeItemPicker
    },

    props: {
        modelValue: {
            type: Object as PropType<ListItemBag | ListItemBag[] | null>,
            required: false
        },

        mergeTemplateOwnership: {
            type: Number as PropType<MergeTemplateOwnership>,
            default: MergeTemplateOwnership.Global
        },
    },

    emits: {
        "update:modelValue": (_value: ListItemBag | ListItemBag[] | null) => true
    },

    setup(props, { emit }) {
        // #region Values

        const internalValue = ref(props.modelValue ?? null);
        const securityGrantToken = useSecurityGrantToken();
        const itemProvider = ref<MergeTemplateTreeItemProvider>();

        // Configure the item provider with our settings.
        function refreshItemProvider(): void {
            const provider = new MergeTemplateTreeItemProvider();
            provider.mergeTemplateOwnership = props.mergeTemplateOwnership;
            provider.securityGrantToken = securityGrantToken.value;

            itemProvider.value = provider;
        }

        refreshItemProvider();

        // #endregion

        // #region Watchers

        watch(() => [props.mergeTemplateOwnership], refreshItemProvider);

        // Keep security token up to date, but don't need refetch data
        watch(securityGrantToken, () => {
            if (itemProvider.value) {
                itemProvider.value.securityGrantToken = securityGrantToken.value;
            }
        });

        watch(internalValue, () => {
            emit("update:modelValue", internalValue.value);
        });

        watch(() => props.modelValue, () => {
            updateRefValue(internalValue, props.modelValue ?? null);
        });

        // #endregion

        return {
            internalValue,
            itemProvider
        };
    },
    template: `
<TreeItemPicker v-model="internalValue"
    formGroupClasses="category-picker"
    iconCssClass="fa fa-files-o"
    :provider="itemProvider"
    :multiple="multiple"
    disableFolderSelection
/>
`
});
