import { reducerFactory as tableReducerFactory, actionsFactory as tableActionsFactory, SELECT_MODE_SINGLE } from './data-table';

export const actions = {
  loadAccountDetails: userId => (dispatch, getState) => {
    dispatch({type: 'accounts/LOAD_DETAIL', payload: { id: userId } });
    fetch(`/admin/api/accounts/${userId}/externallogins`)
      .then(msg => msg.json())
      .then(json => dispatch({type: 'accounts/LOGINS_LOADED', user: userId, payload: json}))
      .catch(err => {
        console.error(err)
        dispatch({type: 'accounts/LOGINS_FAIL', user: userId, failure: err, data: [], meta: {} })
      })
  },
  ...tableActionsFactory('/admin/api/accounts', 'accounts_list', s => s.accounts.list)
};

const tableReducer = tableReducerFactory('accounts_list');

export function reducer(state = { list: { opts: { size: 25, page: 1, selectMode: SELECT_MODE_SINGLE }, data: [] }}, action) {
  switch (action.type) {
    case 'accounts/LOGINS_LOADED':
      var mru = [ ...((state.details || {})._mru || []) ];
      mru.push(action.user);
      var details = (state.details || {})[action.user];
      details = { ...details, logins: action.payload.data }

      // TODO - use _mru to trim the list of accounts
      return {...state, details: { ...state.details, [action.user]: details, _mru: mru }};

    default:
      return {...state, list: tableReducer(state.list, action)};
  }
}