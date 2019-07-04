import { reducerFactory as tableReducerFactory, actionsFactory as tableActionsFactory, SELECT_MODE_SINGLE } from './data-table';

export const actions = {
  ...tableActionsFactory('/api/accounts', 'accounts_list', s => s.accounts.list),

  loadAccountFull: userId => (dispatch, getState) => {
    return actions.loadAccount(userId)(dispatch, getState)
    .then(account => {
      return Promise.all([
        actions.loadAccountDetails(account.data.id, account.data.attributes.memberId)(dispatch, getState),
        actions.loadAccountGroups(account.data.id)(dispatch, getState)
      ]);
    })
  },

  loadAccount: userId => (dispatch, getState) => {
    dispatch({type: 'accounts/LOAD', payload: { id: userId }});
    const task = fetch(`/api/accounts/${userId}`)
    .then(msg => msg.json())
    .then(json => {
      dispatch({type: 'accounts/LOADED', user: userId, payload: json});
      return json;
    });

    task.catch(err => {
      console.error(err);
      dispatch({type: 'accounts/LOAD_FAIL', user: userId, failure: err, data: {}, meta: {}})
    })

    return task;
  },

  loadAccountDetails: (userId, memberId) => (dispatch, getState) => {
    dispatch({type: 'accounts/LOAD_DETAIL', payload: { id: userId } });
    var tasks = [
      fetch(`/api/accounts/${userId}/externallogins`)
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
    return Promise.all(tasks).then(() => dispatch({type: 'accounts/LOADED_DETAIL', user: userId }))
  },

  loadAccountGroups: userId => (dispatch, getState) => {
    dispatch({type: 'accounts/LOAD_GROUPS', payload: { id: userId }});
    const task = fetch(`/api/accounts/${userId}/groups`)
    .then(msg => msg.json())
    .then(json => {
      dispatch({type: 'accounts/GROUPS_LOADED', user: userId, payload: json});
      return json;
    });

    task.catch(err => {
      console.error(err);
      dispatch({type: 'accounts/LOAD_GROUPS_FAIL', user: userId, failure: err, data: {}, meta: {}})
    })

    return task;
  },
};



// ======================  REDUCER  ===================
const tableReducer = tableReducerFactory('accounts_list');
export function reducer(state = { list: { opts: { size: 25, page: 1, selectMode: SELECT_MODE_SINGLE }, data: [] }, details: { data: {} }}, action) {
  switch (action.type) {
    case 'accounts/LOAD_DETAIL':
      return action.payload.id === state.details.id ? state : { ...state, details: { id: action.payload.id }}


    case 'accounts/LOAD':
      return { ...state, details: { ...state.details, isLoading: true }};

    case 'accounts/LOADED':
      return { ...state, details: {...action.payload, id: action.user} };

    case 'accounts/LOGINS_LOADED':
      return action.user !== state.details.id ? state : { ...state, details: { ...state.details, logins: action.payload }};

    case 'accounts/MEMBER_LOADED':
      return action.user !== state.details.id ? state : { ...state, details: { ...state.details, member: action.payload }};

    case 'accounts/GROUPS_LOADED':
        return action.user !== state.details.id ? state : { ...state, details: { ...state.details, groups: action.payload }};
    
    default:
      return {...state, list: tableReducer(state.list, action)};
  }
}