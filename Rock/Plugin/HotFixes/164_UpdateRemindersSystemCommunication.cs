﻿// <copyright>
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

namespace Rock.Plugin.HotFixes
{
    /// <summary>
    /// Plug-in migration
    /// </summary>
    /// <seealso cref="Rock.Plugin.Migration" />
    [MigrationNumber( 164, "1.15.0" )]
    public class UpdateRemindersSystemCommunication : Migration
    {
        /// <summary>
        /// Operations to be performed during the upgrade process.
        /// </summary>
        public override void Up()
        {
            UpdateReminderCommunicationTemplate();
            UpdateDefaultReminderHighlightColors();
        }

        private void UpdateReminderCommunicationTemplate()
        {
            Sql(@"
UPDATE [SystemCommunication]
SET
	  [SMSMessage] = NULL
	, [PushTitle] = NULL
	, [PushMessage] = NULL
	, [PushSound] = NULL
	, [Subject] = 'Reminders for {{ ''Now'' | Date: ''ddd, MMMM d, yyyy'' }}'
	, [Body] = '{% assign peopleReminders = Reminders | Where: ''IsPersonReminder'', true %}
{% assign otherReminders = Reminders | Where: ''IsPersonReminder'', false %}
{% assign currentDate = ''Now'' |  Date:''MMMM d, yyyy'' %}

{{ ''Global'' | Attribute:''EmailHeader'' }}

<h1 style=""margin:0;"">Your Reminders</h1>

<p>
    Below are your reminders for {{ currentDate }}.
</p>

{% if peopleReminders != empty %}
    {% assign entityTypeId = ''0'' %}
    <h2>People</h2>
    <table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
        {% for personReminder in peopleReminders %}
        {% assign entityTypeId = personReminder.EntityTypeId %}
        {% assign reminderDate = personReminder.ReminderDate |  Date:''MMMM d, yyyy'' %}
        {% assign reminderDateIsToday = false %}
        {% if currentDate == reminderDate %}
            {% assign reminderDateIsToday = true %}
        {% endif %}
        {% assign entityUrl = personReminder.EntityUrl %}
        {% if entityUrl == '''' %}
            {% assign entityUrl = '''' %}
        {% endif %}
        <tr>
            <td valign=""middle"" style=""vertical-align:middle;{% if reminderDateIsToday %}background: #f3f4f6;padding-top:8px;padding-left:8px;width:58px !important;{% else %}width:50px !important;{% endif %}"" width=""50""><img src=""{{ personReminder.PersonProfilePictureUrl}}&w=50"" width=""50"" height=""50"" alt="""" style=""display:block;width:50px !important;border-radius:6px;""></td>
            <td style=""vertical-align:middle;padding-left:12px;{% if reminderDateIsToday %}background: #f3f4f6;padding-top:8px;padding-right:8px;{% endif %}"">
                <a href=""{{ entityUrl }}"" style=""font-weight:700"">{{ personReminder.EntityDescription }}</a><br>
                <span style=""font-size:12px;""><span style=""color:{{ personReminder.HighlightColor }}"">&#11044;</span> {{ personReminder.ReminderTypeName }} &middot; {{ reminderDate |  Date:''MMMM d, yyyy'' }}</span>
            </td>
        </tr>
        <tr>
            <td valign=""middle"" style=""vertical-align:middle;{% if reminderDateIsToday %}background: #f3f4f6;width:58px !important;{% else %}width:50px !important;{% endif %}"" width=""50""></td>
            <td style=""padding-top:4px;padding-left:12px;padding-right:12px;{% if reminderDateIsToday %}padding-bottom:12px;background: #f3f4f6;{% endif %}"">
                <p>{{ personReminder.Note | NewlineToBr }}</p>
            </td>
        </tr>
        <tr><td colspan=""2"" style=""padding-bottom:24px;""></td></tr>
        {% endfor %}
        <tr>
            <td colspan=""2"">
                <p style=""text-align:center;""><a href=""{{ ''Global'' | Attribute:''PublicApplicationRoot'' }}reminders/{{ entityTypeId }}"" style=""font-weight:700;text-decoration:underline;"">View All Reminders</a></p>
            </td>
        </tr>
    </table>
{% endif %}


{% assign lastEntityType = '''' %}
{% assign entityTypeId = ''0'' %}
{% if otherReminders != empty %}
    {% if peopleReminders != empty %}
    <hr />
    {% endif %}

    {% for reminder in otherReminders %}
        {% assign entityUrl = reminder.EntityUrl %}
        {% if entityUrl == '''' %}
            {% assign entityUrl = ''#'' %}
        {% endif %}

        {% if lastEntityType != reminder.EntityTypeName %}
            {% if lastEntityType != '''' %}
    <tr>
        <td colspan=""2"" style=""padding-bottom:24px;"">
            <p style=""text-align:center;""><a href=""{{ ''Global'' | Attribute:''PublicApplicationRoot'' }}reminders/{{ entityTypeId }}"" style=""font-weight:700;text-decoration:underline;"">View All Reminders</a></p>
        </td>
    </tr>
</table><hr/>
            {% endif %}
<h2>{{ reminder.EntityTypeName | Pluralize }}</h2>
<table role=""presentation"" cellspacing=""0"" cellpadding=""0"" border=""0"" width=""100%"">
            {% assign lastEntityType = reminder.EntityTypeName %}
            {% assign entityTypeId = reminder.EntityTypeId %}
        {% endif %}

    <tr>
        <td style=""padding-bottom:24px;"">
            <a href=""{{ entityUrl }}"" style=""font-weight:700"">{{ reminder.EntityDescription }}</a><br>
            <span style=""font-size:12px;""><span style=""color:{{ reminder.ReminderType.HighlightColor }}"">&#11044;</span> {{ reminder.ReminderTypeName }} &middot; {{ reminder.ReminderDate | Date:''MMMM d, yyyy'' }}</span>

            <p>{{ reminder.Note | NewlineToBr }}</p>
        </td>
    </tr>

    {% endfor %}
    <tr>
        <td colspan=""2"" style=""padding-bottom:24px;"">
            <p style=""text-align:center;""><a href=""{{ ''Global'' | Attribute:''PublicApplicationRoot'' }}reminders/{{ entityTypeId }}"" style=""font-weight:700;text-decoration:underline;"">View All Reminders</a></p>
        </td>
    </tr>
</table>
{% endif %}

{{ ''Global'' | Attribute:''EmailFooter'' }}'
WHERE
	[Guid] = '7899958C-BC2F-499E-A5CC-11DE1EF8DF20'
");
        }

        private void UpdateDefaultReminderHighlightColors()
        {
            Sql(@"
UPDATE [ReminderType] SET [HighlightColor] = 'rgb(33,159,243)' WHERE [Guid] = 'A9BAAA29-F306-4E35-9273-4B299676B252'
UPDATE [ReminderType] SET [HighlightColor] = 'rgb(76,175,80)' WHERE [Guid] = 'CF5D8F88-8BF0-4880-88BC-102B2AE6159D'
");
        }

        /// <summary>
        /// Operations to be performed during the downgrade process.
        /// </summary>
        public override void Down()
        {
            // Down migrations are not yet supported in plug-in migrations.
        }
    }
}
