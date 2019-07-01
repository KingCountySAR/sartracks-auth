import { reducerFactory as tableReducerFactory, actionsFactory as tableActionsFactory, SELECT_MODE_SINGLE } from './data-table';

export const actions = {
  loadAccountDetails: (userId, memberId) => (dispatch, getState) => {
    dispatch({type: 'accounts/LOAD_DETAIL', payload: { id: userId } });
    var tasks = [
      fetch(`/api/admin/accounts/${userId}/externallogins`)
      .then(msg => msg.json())
      .then(json => dispatch({type: 'accounts/LOGINS_LOADED', user: userId, payload: json}))
      .catch(err => {
        console.error(err)
        dispatch({type: 'accounts/LOGINS_FAIL', user: userId, failure: err, data: [], meta: {} })
      })
    ];

    if (memberId) {
      const state = getState();
      tasks.push(fetch(`${window.reactConfig.apis.data.url}/members/${memberId}`,{
        headers: { 'Authorization': 'Bearer ' + state.oidc.user.access_token }
      })
      .then(msg => msg.json())
      .then(json => {
        const { id, ...attributes } = {id: null, ...json};
        dispatch({type: 'accounts/MEMBER_LOADED', user: userId, payload: { data: { id, attributes }}})
      })
      .catch(err => {
        console.error(err)
        dispatch({type: 'accounts/MEMBER_FAIL', user: userId, failure: err, data: [], meta: {} })
      }));
    } else {
      dispatch({type: 'accounts/MEMBER_LOADED', user: userId, payload: { meta: { notAMember: true }} });
    }
    Promise.all(tasks).then(() => dispatch({type: 'accounts/LOADED_DETAIL', user: userId }))
  },
  ...tableActionsFactory('/api/admin/accounts', 'accounts_list', s => s.accounts.list)
};

const tableReducer = tableReducerFactory('accounts_list');

const processMemberPart = (state, key, action) => {
  var mru = [ ...((state.details || {}).mru || []) ];
  mru.push(action.user);
  var details = (state.details || {})[action.user];
  details = { ...details, [key]: action.payload }

  const detailsList = { ...state.details, mru };
  detailsList[action.user] = details;
  if (mru.length > 10) delete detailsList[mru.shift()];

  return {...state, details: detailsList};
}

export function reducer(state = { list: { opts: { size: 25, page: 1, selectMode: SELECT_MODE_SINGLE }, data: [] }}, action) {
  switch (action.type) {
    case 'accounts/LOGINS_LOADED':
      return processMemberPart(state, 'logins', action);

    case 'accounts/MEMBER_LOADED':
      return processMemberPart(state, 'member', action);

    default:
      return {...state, list: tableReducer(state.list, action)};
  }
}