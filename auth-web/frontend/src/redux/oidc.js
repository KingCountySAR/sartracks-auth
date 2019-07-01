import { reducer as oidc } from 'redux-oidc';

//export const CHECKING = 'oidc/CHECKING';
export const SIGNING_OUT = 'oidc/SIGNING_OUT';
//export const RECEIVE_ROLES = 'RECEIVE_ROLES';

export const ActionsFactory = (userManager) => ({
  doSignout: () => (dispatch) => {
    dispatch({type: SIGNING_OUT })
    userManager.signoutRedirect();
  }
})

export function reducer(state = {}, action) {
  const newState = oidc(state, action)

  switch (action.type) {
    case SIGNING_OUT:
      return ({...newState, isSigningOut: true});

    case 'redux-oidc/SESSION_TERMINATED':
      // Don't remove the isSigningOut flag, or AuthRoute may force sign in again on next render.
      return newState;

    default:
      return newState.isSigningOut ? {...newState, isSigningOut: false } : newState;
  }
}