export const actions = {
  loadApplications: () => (dispatch, getState) => {
    const state = getState()
    const userId = state.oidc.user.profile.sub;
    if (state.me.apps.data.length) return;

    dispatch({type: 'me/LOAD_APPS', payload: { id: userId }});
    return fetch(`/api/accounts/${userId}/applications`)
      .then(msg => msg.json())
      .then(json => dispatch({type: 'me/LOADED_APPS', user: userId, payload: json}))
      .catch(err => {
        console.error(err);
        dispatch({type: 'me/APPS_LOAD_FAIL', user: userId, failure: err, data: [], meta: {}})
      })
  }
};


// ======================  REDUCER  ===================
export function reducer(state = { apps: { data: [] }, ...window.reactConfig.me }, action) {
  switch (action.type) {
    case 'me/LOAD_APPS':
      return {...state, apps: { ...state.apps, loading: true, failed: false } };

    case 'me/LOADED_APPS':
      return {...state, apps: action.payload };

    case 'me/APPS_LOAD_FAIL':
      return {...state, apps: { ...action.payload, failed: true }};

    default:
      return state;
  }
}