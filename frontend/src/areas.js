export const areas = [
  {
    route: 'app-administration',
    title: 'App administration',
    purpose: 'Set up subscription models, register tenants and add users.',
    scope: null,
  },
  {
    route: 'tenant-administration',
    title: 'Tenant administration',
    purpose: 'Rename your tenant and add the teams that work in it.',
    scope: 'tenant',
  },
  {
    route: 'team-administration',
    title: 'Team administration',
    purpose: 'Add projects and the languages they are translated into.',
    scope: 'team',
  },
  {
    route: 'translation-editor',
    title: 'Translation editor',
    purpose: 'Add keys and write their texts in every language.',
    scope: 'project',
  },
  {
    route: 'translation-delivery',
    title: 'Translation delivery',
    purpose: 'See the texts your application receives in each language.',
    scope: 'project',
  },
]
